"""
Abstract class representing any game character.

Created on Jun 11, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import glob
import os
import random

import pygame

import animation
import game_constants
import powerup

# Enum of possible character action states.
STAND = 1
WALK = 2
RUN = 3
JUMP = 4
FALL= 5
GROUNDED = 6
LEFT = game_constants.LEFT
RIGHT = game_constants.RIGHT
ATTACK = game_constants.ATTACK
PATH = os.path.join('media', 'sprites')

class Character(pygame.sprite.Sprite):
  """Characters include the PC, NPCs, and enemies.
  
  Attributes:
    hp: int current hit points.
    invulnerable: int number of frames this character is invulnerable for. Negative numbers mean
      permanent invulnerability.
    solid: boolean whether or not other Characters can pass through this one.
    harmful: boolean whether colliding with this Character causes damage.
    image: pygame.Surface. Required for all Sprite subclasses, should be overridden by the child.
    rect: pygame Rect object representing the sprite's position.
    movement: [x, y] pair representing the character's rate of movement.  (Positive numbers
      are right and down).
    direction: int LEFT or RIGHT, the direction the character is currently facing.
    horizontal: int STAND, WALK, or RUN, the move action in the horizontal direction.
    vertical: int JUMP, FALL, or GROUNDED, the move action in the vertical direction.
    state: (direction, action) tuple describing what the character is doing.
    last_state: (direction, action) tuple for the previous frame.
  """
  
  # Override these in child classes to change behavior.
  STARTING_HP = 1
  SOLID = True
  INVULNERABLE = False
  INVULNERABILITY_FRAMES = 30
  GRAVITY = 0
  TERMINAL_VELOCITY = 0
  ACCEL = 100
  SPEED = 0
  JUMP_DURATION = 0
  WIDTH = 48
  HEIGHT = 48
  SIZE = (WIDTH, HEIGHT)
  DEFAULT_STATE = (STAND, LEFT)
  IMAGE = None
  IMAGE_FILE = 'nothing'
  PUSHBACK = 16
  DAMAGE = 0
  IS_PLAYER = False
  ITEM_DROPS = [powerup.HealthRestore, powerup.AmmoRestore]
  DROP_PROBABILITY = 10

  HIT_SOUND = pygame.mixer.Sound(os.path.join('media', 'sfx', 'hit.wav'))
  DEATH_SOUND = pygame.mixer.Sound(os.path.join('media', 'sfx', 'death.wav'))

  def __init__(self, environment, position=(0, 0)):
    """Constructor.
    
    Args:
      environment: Environment object this character appears in.
      position: Initial (x, y) tile position for this character.  The top left corner of the
        character will be aligned with this tile.
    """
    print position
    pygame.sprite.Sprite.__init__(self)
    self.env = environment
    map_rect = self.env.RectForTile(*position)
    screen_coordinates = self.env.ScreenCoordinateForMapPoint(map_rect.left, map_rect.bottom)
    self.rect = pygame.Rect((screen_coordinates[0], 0), (self.WIDTH, self.HEIGHT))
    # Align the bottoms of the sprite and tile - this allows easier placement for things that
    # are more than one tile tall.
    self.rect.bottom = screen_coordinates[1]
    self.action, self.direction = self.DEFAULT_STATE
    self.vertical = FALL
    self.movement = [0, 0]
    self.max_hp = self.STARTING_HP
    self.jump_duration = self.JUMP_DURATION
    self.hp = self.STARTING_HP
    self.invulnerable = 0
    self.solid = True
    self.InitImage()

  @property
  def state(self):
    return (self.action, self.direction)

  def InitImage(self):
    raise NotImplementedError('Each subclass of Character must implement InitImage.')

  def Hitbox(self):
    """Gets the Map hitbox for the sprite, which is relative to the map rather than the screen.
    
    The Hitbox needs to be smaller than the sprite, partly because of weird PyGame behavior
    where a rect of width X and height y actually touches (x+1) * (y+1) pixels.
    """
    x, y = self.env.MapCoordinateForScreenPoint(self.rect.left, self.rect.top)
    # TODO: Make these offsets constants so they can be configured per-class?
    return pygame.Rect(x + 1, y + 1, self.rect.width - 2, self.rect.height - 2)

  def SenseAndReturnHitbox(self, player):
    """Allows the character to be aware of the player's position, and then returns hitbox.
    
    Must override if you want the enemy to actually do anything with that knowledge.
    """
    return self.Hitbox()

  def SetCurrentImage(self):
    """Set self.image to the appropriate value.  Should be overriden for classes with animation."""
    self.image = self.IMAGE

  def FlickerIfInvulnerable(self):
    """Make the character flicker if they are currently invulnerable."""
    if self.invulnerable > 0 and self.invulnerable % 4 > 0:
      self.image.set_alpha(128)
    else:
      self.image.set_alpha(255)
    
  def Walk(self, direction):
    if self.action != JUMP:
      self.action = WALK
    if direction == LEFT:
      self.direction = LEFT
      # Accelerate or decelerate to movement speed over time.
      if self.movement[0] < -self.SPEED:
        self.movement[0] += self.GRAVITY
      else:
        self.movement[0] = max(self.movement[0] - self.ACCEL, -self.SPEED)
    elif direction == RIGHT:      
      self.direction = RIGHT
      if self.movement[0] > self.SPEED:
        self.movement[0] -= self.GRAVITY
      else:
        self.movement[0] = min(self.movement[0] + self.ACCEL, self.SPEED)

  def Gravity(self):
    """Apply the force of gravity to the character.
    
    This should only be called when the character is in the air (in JUMP action).
    """
    if self.movement[1] < self.TERMINAL_VELOCITY:
      self.movement[1] = min(self.movement[1] + self.GRAVITY, self.TERMINAL_VELOCITY)

  def Supported(self):
    """The character is standing on something solid."""
    self.vertical = GROUNDED
    self.movement[1] = 0
    
  def GetMove(self):
    """Method for getting the unit's movement vector.
    
    Must be implemented by any subclass that doesn't also override update().
    """
    raise NotImplementedError()
  
  def GetDistance(self, other):
    """Calculate the distance between this and another character."""
    return ((self.Hitbox().centerx - other.Hitbox().centerx) ** 2
            + (self.Hitbox().centery - other.Hitbox().centery) ** 2) ** 0.5
  
  def TakeHit(self, damage):
    """Take a hit for a given amount of damage."""
    self.HIT_SOUND.play()
    self.hp -= damage
    print self.hp
    if self.hp <= 0:
      self.Die()
    else:
      self.invulnerable = self.INVULNERABILITY_FRAMES
      
  def Die(self):
    """This character dies."""
    self.DEATH_SOUND.play()
    if len(self.ITEM_DROPS) > 0:
      if random.randint(0, 100) < self.DROP_PROBABILITY:
        position = self.env.TileIndexForPoint(
            *self.env.MapCoordinateForScreenPoint(self.rect.centerx, self.rect.centery))
        drop = random.choice(self.ITEM_DROPS)(self.env, position)
        self.env.item_group.add(drop)
    self.env.dying_animation_group.add(Dying(self.rect))
    self.kill()
  
  def CollisionPushback(self, other):
    """Calculate and apply a movement vector for being hit by another character."""
    pushback_x = self.rect.centerx - other.rect.centerx
    pushback_y = self.rect.centery - other.rect.centery
    pushback_scalar = other.PUSHBACK / (float(pushback_x ** 2 + pushback_y ** 2) ** 0.5)
    self.movement[0] += int(pushback_x * pushback_scalar)
    self.movement[1] += int(pushback_y * pushback_scalar)
    print '%s is getting knocked back! %s' % (type(self), self.movement)

  def RaiseMaxHp(self, amount):
    """Raises the character's max HP and also recovers their current HP by the same amount."""
    self.max_hp += amount
    self.RecoverHealth(amount)

  def RecoverHealth(self, amount):
    """Heals a specific amount of HP."""
    self.hp = min(self.max_hp, self.hp + amount)

  def WalkBackAndForth(self):
    """Get movement for walking back and forth on the current platform occupied."""
    if self.movement[0] <= 0:
      self.Walk(LEFT)
      dest_tile = self.env.TileIndexForPoint(
          self.Hitbox().left + self.movement[0], self.Hitbox().bottom)
    elif self.movement[0] > 0:
      self.Walk(RIGHT)
      dest_tile = self.env.TileIndexForPoint(
          self.Hitbox().right + self.movement[0], self.Hitbox().bottom)
      
    # Check boundaries using existing AttemptMove method.  Kinda ugly.
    new_rect = self.env.IsMoveLegal(self, self.movement)

    if new_rect == self.rect or not self.env.IsTileSupported(*dest_tile):
      if self.direction == LEFT:
        self.Walk(RIGHT)
      else:
        self.Walk(LEFT)
    return self.movement

  def update(self):
    new_rect = self.env.AttemptMove(self, self.GetMove())
    self.rect = new_rect
    if self.env.IsRectSupported(self.Hitbox()):
      self.Supported()
    else:
      self.Gravity()
    self.SetCurrentImage()
    self.FlickerIfInvulnerable()
    if self.invulnerable > 0:
      self.invulnerable -= 1
    self.last_state = self.state
    if self.env.IsOutsideMap(self.Hitbox()):
      self.kill()
    
  def __repr__(self):
    str(type(self))


def LoadImage(filename, default_width=game_constants.TILE_WIDTH, 
              default_height=game_constants.TILE_HEIGHT, scaled=False, colorkey=None):
  """Load and return a sprite image from its filename."""
  try:
    return LoadImages(filename, scaled=scaled, colorkey=colorkey)[0]
  except pygame.error as e:
    print filename, e
    placeholder = pygame.Surface((default_width, default_height)).convert_alpha()
    placeholder.fill(game_constants.WHITE)
    return placeholder


def LoadImages(fileglob, scaled=False, colorkey=None):
  """Load and return a list of Surface objects matching the passed in pattern.
  
  Args:
    fileglob: String pattern of files to look for in media/sprites.
    scaled: True if the image should be scaled up (most game assets are shown at 3x).
    colorkey: RGB value to use for transparent.  If none, uses per-pixel alpha instead.
  """

  images = []
  for filename in sorted(glob.glob(os.path.join(PATH, fileglob))):
    image = pygame.image.load(filename)
    if scaled:
      image = pygame.transform.scale(image, (image.get_width() * 3, image.get_height() * 3))
    if colorkey is not None:
      image.set_colorkey(colorkey)
      images.append(image.convert())
    else:
      images.append(image.convert_alpha())
  return images

def CollideCharacters(player, enemy):
  """Return True if two characters collide, otherwise false.
  
  This may produce side effects - it's the easiest way to let enemies know where the player
  is, since it's called once per frame.
  """
  return player.Hitbox().colliderect(enemy.SenseAndReturnHitbox(player))


class Dying(pygame.sprite.Sprite):
  """Not actually a character, just a dying animation left behind by one."""

  IMAGES = None
  
  def __init__(self, rect):
    """Constructor.
    
    Args:
      rect: The rect of the enemy that is dying.
    """
    pygame.sprite.Sprite.__init__(self)
    self.InitImage()
    self.rect = pygame.Rect((0, 0), self.image.get_size())
    self.rect.centerx, self.rect.centery = rect.centerx, rect.centery
    self.death_frames = 20
  
  def InitImage(self):
    if Dying.IMAGES is None:
      Dying.IMAGES = LoadImages('regularexplode1*.png', scaled=True,
                                colorkey=game_constants.SPRITE_COLORKEY)
    self.animation = animation.Animation(Dying.IMAGES, looping=False, framedelay=3)
    self.SetCurrentImage()

  def Hitbox(self):
    return pygame.Rect((0, 0), (0, 0))

  def SetCurrentImage(self):
    self.image = self.animation.NextFrame()
  
  def update(self):
    self.SetCurrentImage()
    if self.death_frames == 0:
      self.kill()
    else:
      self.death_frames -= 1