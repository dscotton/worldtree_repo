"""
Powerup classes.

Powerups are non-character sprites that have a one-time effect when acquired, generally changing
a player's stat.  They can be placed on the map by design, in which case they will be removed
after being picked up, or they can be dropped by enemies.

Created on Jun 16, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os
import time

import pygame

import animation
import character
import game_constants
import map_data

class Powerup(pygame.sprite.Sprite):
  """An item picked up by the player that has an effect on his stats.
  
  Subclasses must initialize a surface for self.image.
  """
  
  # If subclassed object is a different size, it needs to update its rect attribute.
  WIDTH = 48
  HEIGHT = 48
  IMAGE_FILE = 'orb.png'
  IMAGE_FILES = None  # A file pattern used to glob images.
  IMAGE = None
  IMAGES = None

  def __init__(self, environment, position, one_time=True, cleanup=False, sound=None):
    """Constructor.

    Args:
      environment: Environment object this powerup appears in.
      position: Initial (x, y) tile position for this item.  The top left corner of the
        item will be aligned with this tile.
      one_time: Boolean, if True this item will be removed from the screen after it's encountered.
      cleanup: Boolean, if True the object will be removed from map_data after it is picked up.
      sound: pygame.mixer.Sound object to play when the item is picked up.
    """
    pygame.sprite.Sprite.__init__(self)
    self.env = environment
    self.col = position[0]
    self.row = position[1]
    self.one_time = one_time
    self.cleanup = cleanup
    self.sound = sound
    self.dead = False
    map_rect = self.env.RectForTile(*position)
    self.rect = pygame.Rect(self.env.ScreenCoordinateForMapPoint(map_rect.left, map_rect.top),
                            (self.WIDTH, self.HEIGHT))
    self.InitImage()
    if self.IMAGE is not None:
      self.image = self.IMAGE
    elif self.IMAGES is not None:
      self.animation = animation.Animation(self.IMAGES, framedelay=1)
      self.image = self.animation.NextFrame()

  @classmethod
  def InitImage(cls):
    if cls.IMAGE is None and cls.IMAGE_FILE is not None:
      cls.IMAGE = character.LoadImage(cls.IMAGE_FILE, scaled=True)
    elif cls.IMAGES is None and cls.IMAGE_FILES is not None:
      cls.IMAGES = character.LoadImages(cls.IMAGE_FILES, scaled=True,
                                        colorkey=game_constants.SPRITE_COLORKEY)

  def Hitbox(self):
    """Gets the Map hitbox for the sprite, which is relative to the map rather than the screen.
    
    The Hitbox needs to be smaller than the sprite, partly because of weird PyGame behavior
    where a rect of width X and height y actually touches (x+1) * (y+1) pixels.
    """
    x, y = self.env.MapCoordinateForScreenPoint(self.rect.left, self.rect.top)
    # TODO: Make these offsets constants so they can be configured per-class?
    return pygame.Rect(x + 3, y + 3, self.rect.width - 6, self.rect.height - 6)

  def update(self):
    if self.IMAGES is not None:
      self.image = self.animation.NextFrame()

  def Use(self, player):
    raise NotImplementedError('Subclasses must define the effect of the powerup.')

  def PickUp(self, player):
    """Handle the object being acquired by the player."""
    if self.sound is not None:
      pygame.mixer.music.pause()
      channel = self.sound.play()
      while channel.get_busy():
        # TODO: consider whether we want this to happen or not.  This freezes the game while
        # playing the jingle!
        time.sleep(0.1)
      pygame.mixer.music.unpause()
    self.Use(player)
    if self.cleanup:
      map_data.map_data[self.env.name]['mapcodes'][self.row][self.col] = 0
    if self.one_time:
      self.env.dirty = True  # Needed to make the image vanish right away.
      self.kill()

    
class HealthBoost(Powerup):
  
  HEALTH_BONUS = 5
  SOUND = pygame.mixer.Sound(os.path.join(game_constants.MUSIC_DIR, 'item_get.ogg'))
  IMAGE_FILE = None
  IMAGE_FILES = 'lifeup*.png'
  
  def __init__(self, environment, position):
    Powerup.__init__(self, environment, position, cleanup=True, sound=HealthBoost.SOUND)
    
  def Use(self, player):
    player.RaiseMaxHp(self.HEALTH_BONUS)


class HealthRestore(Powerup):
  
  HEALTH_BONUS = 3
  IMAGE_FILE = None
  IMAGE_FILES = 'healthrestore*.png'

  def __init__(self, environment, position):
    Powerup.__init__(self, environment, position)
    print 'creating powerup at %s' % (position,)
    
  def Use(self, player):
    player.RecoverHealth(self.HEALTH_BONUS)


class DoubleJump(Powerup):
  
  SOUND = pygame.mixer.Sound(os.path.join(game_constants.MUSIC_DIR, 'item_get.ogg'))
  IMAGE = None
  
  def __init__(self, environment, position):
    Powerup.__init__(self, environment, position, cleanup=True, sound=DoubleJump.SOUND)

  def Use(self, player):
    player.max_jumps = 2
    print 'Now you can double jump!'
    

class MoreSeeds(Powerup):
  """Powerup that increases the player's maximum ammo."""

  SOUND = pygame.mixer.Sound(os.path.join(game_constants.MUSIC_DIR, 'item_get.ogg'))
  IMAGE_FILE = None
  IMAGE_FILES = 'ammoup*.png'
  
  def __init__(self, environment, position):
    Powerup.__init__(self, environment, position, cleanup=True, sound=MoreSeeds.SOUND)

  def Use(self, player):
    player.max_ammo += 2
    player.ammo = min(player.max_ammo, player.ammo + 2)


class AmmoRestore(Powerup):
  """Powerup that refills some of the player's ammo."""

  IMAGE_FILE = None
  IMAGE_FILES = 'seedammo*.png'

  def __init__(self, environment, position):
    Powerup.__init__(self, environment, position)

  def Use(self, player):
    player.ammo = min(player.max_ammo, player.ammo + 2)


class Lava(Powerup):
  """An area that inflicts damage when the player stands in it.
  
  Although the name is confusing, this is a "Powerup" because it is a static object that
  doesn't get updated each frame.  If I were starting over I'd probably rename the Powerup class.
  
  This class is also handled somewhat differently from other powerups in that the game will
  try to join adjacent Lava tiles into a single object to reduce the number amount of collision
  checks taking place each frame.
  """
  
  IMAGE_FILE = 'transparent.png'
  DAMAGE = 2

  def __init__(self, environment, position, size):
    """Constructor.
    
    Args:
      environment: Environment object this Lava exists in.
      position: (x, y) tile position of the Lava.
      size: (width, height) of the lava in tiles.
    """
    Powerup.__init__(self, environment, position, one_time=False, cleanup=False)
    self.rect.width = size[0] * game_constants.TILE_WIDTH
    self.rect.height = size[1] * game_constants.TILE_HEIGHT
    
  def Use(self, player):
    if player.invulnerable == 0:
      player.TakeHit(self.DAMAGE)


class Spike(Powerup):
  """An area that inflicts damage and pushes the player back when the player touches it.
  
  As with Lava, the game will try to join adjacent Spike tiles into a single object to reduce
  the number amount of collision checks taking place each frame.
  """
  
  IMAGE_FILE = 'transparent.png'
  DAMAGE = 2
  PUSHBACK = 32

  def __init__(self, environment, position, size):
    """Constructor.
    
    Args:
      environment: Environment object this Spike exists in.
      position: (x, y) tile position of the Spike.
      size: (width, height) of the Spike in tiles.
    """
    Powerup.__init__(self, environment, position, one_time=False, cleanup=False)
    self.rect.width = size[0] * game_constants.TILE_WIDTH
    self.rect.height = size[1] * game_constants.TILE_HEIGHT
    
  def Use(self, player):
    if player.invulnerable == 0:
      player.CollisionPushback(self)
      player.TakeHit(self.DAMAGE)


def CollideSprites(player, other):
  """Return True if two characters collide, otherwise false.
  
  This may produce side effects - it's the easiest way to let enemies know where the player
  is, since it's called once per frame.
  """
  return player.Hitbox().colliderect(other.Hitbox())