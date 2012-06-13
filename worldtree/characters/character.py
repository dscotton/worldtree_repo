"""
Abstract class representing any game character.

Created on Jun 11, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os

import pygame

import controller
import game_constants

# Enum of possible character action states.
STAND = 1
WALK = 2
JUMP = 3
RUN = 4
LEFT = controller.LEFT
RIGHT = controller.RIGHT
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
    state: (direction, action) tuple describing what the character is doing.
    last_state: (direction, action) tuple for the previous frame.
  """
  
  # Override these in child classes to change behavior.
  STARTING_HP = 1
  SOLID = True
  INVULNERABLE = False
  HARMFUL = False
  GRAVITY = 0
  TERMINAL_VELOCITY = 0
  SPEED = 0
  WIDTH = 32
  HEIGHT = 32
  SIZE = (WIDTH, HEIGHT)
  DEFAULT_STATE = (STAND, LEFT)
  STARTING_MOVEMENT = [0, 0]
  IMAGE = None
  IMAGE_FILE = 'nothing'

  def __init__(self, environment, position=(0, 0)):
    """Constructor.
    
    Args:
      environment: Environment object this character appears in.
      position: Initial (x, y) tile position for this character.  The top left corner of the
        character will be aligned with this tile.
    """
    pygame.sprite.Sprite.__init__(self)
    self.env = environment
    map_rect = self.env.RectForTile(*position)
    self.rect = pygame.Rect(self.env.ScreenCoordinateForMapPoint(map_rect.left, map_rect.top),
                            (self.WIDTH, self.HEIGHT))
    self.action, self.direction = self.DEFAULT_STATE
    self.movement = self.STARTING_MOVEMENT
    self.hp = self.STARTING_HP
    self.invulnerable = False
    self.solid = True
    self.InitImage()

  @property
  def state(self):
    return (self.action, self.direction)

  def InitImage(self):
    if Character.IMAGE is None:
      raw_image = LoadImage(self.IMAGE_FILE, default_width=self.WIDTH, default_height=self.HEIGHT)
      Character.IMAGE = pygame.transform.scale(raw_image, self.SIZE).convert_alpha()
    self.image = self.IMAGE

  def Hitbox(self):
    """Gets the Map hitbox for the sprite, which is relative to the map rather than the screen.
    
    The Hitbox needs to be smaller than the sprite, partly because of weird PyGame behavior
    where a rect of width X and height y actually touches (x+1) * (y+1) pixels.
    """
    x, y = self.env.MapCoordinateForScreenPoint(self.rect.left, self.rect.top)
    # TODO: Make these offsets constants so they can be configured per-class?
    return pygame.Rect(x + 1, y + 1, self.rect.width - 2, self.rect.height - 2)

  def SetCurrentImage(self):
    """Set self.image to the appropriate value.  Should be overriden for classes with animation."""
    self.image = self.IMAGE
    
  def Walk(self, direction):
    if self.action != JUMP:
      self.action = WALK
    if direction == LEFT:
      self.direction = LEFT
      self.movement[0] = -self.SPEED
    elif direction == RIGHT:      
      self.direction = RIGHT
      self.movement[0] = self.SPEED

  def Gravity(self):
    """Apply the force of gravity to the character.
    
    This should only be called when the character is in the air (in JUMP action).
    """
    if self.movement[1] < self.TERMINAL_VELOCITY:
      self.movement[1] = min(self.movement[1] + self.GRAVITY, self.TERMINAL_VELOCITY)

  def Supported(self):
    """The character is standing on something solid. This is the only way to leave JUMP action."""
    self.action = STAND
    self.movement[1] = 0
    
  def GetMove(self):
    """Method for getting the unit's movement vector.
    
    Must be implemented by any subclass that doesn't also override update().
    """
    raise NotImplementedError()

  def WalkBackAndForth(self):
    """Get movement for walking back and forth on the current platform occupied."""
    new_rect = self.env.AttemptMove(self, self.movement)
    if new_rect == self.rect:
      # TODO: Figure out why this stops from entering an unsupported tile instead of stopping
      # from going all the way over the edge.
      self.movement[0] = -self.movement[0]
    return self.movement

  def update(self):
    new_rect = self.env.AttemptMove(self, self.GetMove())
    if self.env.IsRectSupported(self.Hitbox()):
      self.Supported()
    else:
      # If the character actually is falling, set them in jump status.
      self.action = JUMP
      self.Gravity()
    self.SetCurrentImage()
    self.last_state = self.state

    self.rect = new_rect
    
  def __repr__(self):
    str(type(self))


def LoadImage(filename, default_width=game_constants.TILE_WIDTH, 
              default_height=game_constants.TILE_HEIGHT):
  """Load and return a sprite image from its filename."""
  # TODO: This should really be in a shared class.
  try:
    return pygame.image.load(os.path.join(PATH, filename)).convert_alpha()
  except pygame.error:
    placeholder = pygame.Surface((default_width, default_height)).convert_alpha()
    placeholder.fill(game_constants.WHITE)
    return placeholder
