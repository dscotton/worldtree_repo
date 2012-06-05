"""
This class represents the player character.

TODO(dscotton): Much of this code should probably be moved up a level to be shared with
enemies, but I'm not yet sure what's needed, so I'm putting it here for now.

Created on Jun 2, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os

import pygame
import controller

# TODO(dscotton): These almost certainly belong a level higher, in a shared character class.
# Descriptive constants
STAND = 1
WALK = 2
JUMP = 3
RUN = 4
LEFT = controller.LEFT
RIGHT = controller.RIGHT
PATH = os.path.join('media', 'sprites')

class Hero(pygame.sprite.Sprite):
  """The player character.
  
  Attributes:
    image: Surface object that currently represents the character.  Should be changed
      to the appropriate value in the images dict as the character moves.
    environment: an Environment class representing the area the character is in.
    direction: int LEFT or RIGHT, the direction the character is currently facing.
    rect: pygame Rect object representing the sprite's position.
    movement: [x, y] pair representing the character's rate of movement.  (Positive numbers
      are right and down).
    state: (direction, action) tuple describing what the character is doing.
    last_state: (direction, action) tuple for the previous frame.
    animation_frame: int for the frame of animation being shown in a particular action.
      
  Constants:
    SIZE: default (x, y) size of the character. Overridden by the actual size of the current image.
    COLOR: default (R, B, G) color of the character. Only relevant in error cases.
    DEFAULT_STATE: default (action, direction) of the character. Relevant for picking sprite.
    SPEED: Normal walking speed.
    JUMP: Jumping power.
    GRAVITY: Rate of downward acceleration while falling.
    TERMINAL_VELOCITY: Maximum rate at which you can fall.
  """

  # Actual size will be determined by the current image surface, but this is a 
  # default and guideline.
  WIDTH = 32
  HEIGHT = 64
  SIZE = (WIDTH, HEIGHT)
  COLOR = (0x00, 0xFF, 0x66)
  DEFAULT_STATE = (STAND, LEFT)
  SPEED = 4
  JUMP = 24
  GRAVITY = 2
  TERMINAL_VELOCITY = 8
    
  def __init__(self, environment, position=(0, 0)):
    pygame.sprite.Sprite.__init__(self)
  
    # Load the sprite graphics once so we don't need to reload on the fly.
    # TODO: These ideally would be class constants, but because of the dependency on LoadImage
    # they can't be without ugly formatting.  See if it's possibly by moving LoadImage to
    # a higher level.
    self.STAND_LEFT = Hero.LoadImage('hero_stand_left.png')
    self.STAND_RIGHT = pygame.transform.flip(self.STAND_LEFT, True, False)

    self.environment = environment
    self.action, self.direction = self.DEFAULT_STATE
    self.image = self.STAND_LEFT
    self.rect = self.image.get_rect()
    self.rect.left, self.rect.top = position
    self.movement = [0, 0]
    self.last_state = (self.direction, self.action)
    self.animation_frame = 0

  @property
  def state(self):
    return (self.action, self.direction)

  def Hitbox(self):
    """Get a Hitbox for the sprite, which is smaller than the sprite's rect.
    
    The Hitbox needs to be smaller than the sprite, partly because of weird PyGame behavior
    where a rect of width X and height y actually touches (x+1) * (y+1) pixels.
    """
    return pygame.Rect(self.rect.left+1, self.rect.top+1, self.WIDTH-2, self.HEIGHT-2)
    
  @classmethod
  def LoadImage(cls, filename):
    """Load and return a sprite image from its filename."""
    # TODO: This should really be in a shared class.
    try:
      return pygame.image.load(os.path.join(PATH, filename)).convert_alpha()
    except pygame.error:
      placeholder = pygame.Surface(cls.SIZE).convert_alpha()
      placeholder.fill(cls.COLOR)
      return placeholder
    
  def HandleInput(self):
    """Handles user input to move the character and change his action."""
    actions = controller.GetInput()
    if ((controller.LEFT in actions and controller.RIGHT in actions) or
        (controller.LEFT not in actions and controller.RIGHT not in actions)):
      self.StopMoving()
    elif controller.LEFT in actions:
      self.Walk(LEFT)
    elif controller.RIGHT in actions:
      self.Walk(RIGHT)
    if controller.JUMP in actions:
      self.Jump()
    else:
      self.StopUpwardMovement()
  
  def Walk(self, direction):
    if self.action != JUMP:
      self.action = WALK
    if direction == LEFT:
      self.direction = LEFT
      self.movement[0] = -self.SPEED
    elif direction == RIGHT:      
      self.direction = RIGHT
      self.movement[0] = self.SPEED
    
  def StopMoving(self):
    if self.action != JUMP:
      self.action = STAND
    self.movement[0] = 0
    
  def StopUpwardMovement(self):
    if self.action == JUMP and self.movement[1] < 0:
      self.movement[1] = 0
  
  def Jump(self):
    if self.action != JUMP:
      self.movement[1] = self.movement[1] - self.JUMP
    self.action = JUMP
    
  def Supported(self):
    """The character is standing on something solid. This is the only way to leave JUMP action."""
    self.action = STAND
    self.movement[1] = 0
    
  def Gravity(self):
    """Decreases the character's upward momentum.
    
    This should only be called when the character is in the air (in JUMP action).
    """
    if self.movement[1] < self.TERMINAL_VELOCITY:
      self.movement[1] = min(self.movement[1] + self.GRAVITY, self.TERMINAL_VELOCITY)

  def SetCurrentImage(self):
    """Sets the image to the appropriate one for the current action, if it exists.
    
    This method also handles cycling through frames of animation if there is more than
    one image for a given state.
    """
    # A dict of state->image seemed nice and pythonic at first, but it's not nearly as good
    # at handling cases where we don't want a different image for every possible state. This
    # way we can only apply animation logic when there are multiple frames to animate.
    if LEFT == self.direction:
      self.image = self.STAND_LEFT
    elif RIGHT == self.direction:
      self.image = self.STAND_RIGHT

  def update(self):
    # TODO: It is now possible to walk off the edge of the screen if it isn't blocked by an
    # impassible tile - should this be fixed?
    new_rect = self.environment.AttemptMove(self, self.movement)
    if self.movement[1] > 0 and new_rect.bottom == self.rect.bottom:
      # If y momentum is positive but the environment didn't let hero move down, this implies
      # the character is on solid ground and no longer falling.
      self.Supported()
    elif not self.environment.IsRectSupported(self.Hitbox()):
      # If the character actually is falling, set them in jump status.
      self.action = JUMP
      self.Gravity()
    self.SetCurrentImage()
    self.last_state = self.state

    self.rect = new_rect
