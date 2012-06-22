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
  IMAGE = None

  def __init__(self, environment, position, cleanup=False, sound=None):
    """Constructor.
    
    Args:
      environment: Environment object this powerup appears in.
      position: Initial (x, y) tile position for this item.  The top left corner of the
        item will be aligned with this tile.
      cleanup: Boolean, if True the object will be removed from map_data after it is picked up.
      sound: pygame.mixer.Sound object to play when the item is picked up.
    """
    pygame.sprite.Sprite.__init__(self)
    self.env = environment
    self.col = position[0]
    self.row = position[1]
    self.cleanup = cleanup
    self.sound = sound
    self.dead = False
    map_rect = self.env.RectForTile(*position)
    self.rect = pygame.Rect(self.env.ScreenCoordinateForMapPoint(map_rect.left, map_rect.top),
                            (self.WIDTH, self.HEIGHT))
    if self.IMAGE is None:
      self.IMAGE = character.LoadImage('orb.png', scaled=True)
    self.image = self.IMAGE

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
    self.env.dirty = True  # Needed to make the image vanish right away.
    self.kill()
    
    
class HealthBoost(Powerup):
  
  HEALTH_BONUS = 10
  SOUND = pygame.mixer.Sound(os.path.join(game_constants.MUSIC_DIR, 'jingle.ogg'))
  IMAGE = None
  
  def __init__(self, environment, position):
    Powerup.__init__(self, environment, position, cleanup=True, sound=HealthBoost.SOUND)
    
  def Use(self, player):
    player.max_hp += self.HEALTH_BONUS
    player.hp += self.HEALTH_BONUS
    print 'New HP is %s out of %s' % (player.hp, player.max_hp)
    
    
class DoubleJump(Powerup):
  
  SOUND = pygame.mixer.Sound(os.path.join(game_constants.MUSIC_DIR, 'jingle.ogg'))
  IMAGE = None
  
  def __init__(self, environment, position):
    Powerup.__init__(self, environment, position, cleanup=True, sound=DoubleJump.SOUND)

  def Use(self, player):
    player.max_jumps = 2
    print 'Now you can double jump!'
    

class MoreSeeds(Powerup):
  """Powerup that increases the player's maximum ammo."""

  SOUND = pygame.mixer.Sound(os.path.join(game_constants.MUSIC_DIR, 'jingle.ogg'))
  IMAGE = None
  
  def __init__(self, environment, position):
    Powerup.__init__(self, environment, position, cleanup=True, sound=MoreSeeds.SOUND)

  def Use(self, player):
    player.max_ammo += 5
    player.ammo += 5
