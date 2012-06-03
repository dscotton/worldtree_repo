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
LEFT = 1
RIGHT = 2

class Hero(pygame.sprite.Sprite):
  """The player character.
  
  Attributes:
    images: dict of (state, direction) to list of image Surface objects to show for that
      state.  If there are multiples, animate by looping through them in order for as
      long as the state, direction pair is applicable, resetting if it changes.
    image: Surface object that currently represents the character.  Should be changed
      to the appropriate value in the images dict as the character moves.
    environment: the Rect object of the screen the character is moving through.
    direction: int LEFT or RIGHT, the direction the character is currently facing.
    rect: pygame Rect object representing the sprite's position.
    movement: [x, y] pair representing the character's rate of movement.  (Positive numbers
      are right and down).
      
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
  SIZE = (16, 32)
  COLOR = (0x00, 0xFF, 0x66)
  DEFAULT_STATE = (STAND, LEFT)
  SPEED = 4
  JUMP = 24
  GRAVITY = 2
  TERMINAL_VELOCITY = 8
  
  def __init__(self, position=(0, 0)):
    pygame.sprite.Sprite.__init__(self)
    # Load all sprite graphics here.
    self.images = {
      (STAND, LEFT) : [os.path.join('media', 'sprites', 'hero_stand_left.png')]
    }
    for key in self.images:
      try:
        self.images[key] = [pygame.image.load(image) for image in self.images[key]]
      except pygame.error:
        placeholder = pygame.Surface(self.SIZE)
        placeholder.fill(self.COLOR)
        self.images[key] = [placeholder]
        
    self.environment = pygame.display.get_surface().get_rect()
    self.direction, self.state = self.DEFAULT_STATE
    self.image = self.images[(self.direction, self.state)][0]
    self.rect = self.image.get_rect()
    self.rect.left, self.rect.top = position
    self.movement = [0, 0]
    
  def HandleInput(self):
    """Handles user input to move the character and change his state."""
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
      self.StopJumping()
  
  def Walk(self, direction):
    if self.state != JUMP:
      self.state = WALK
    if direction == LEFT:
      self.movement[0] = -self.SPEED
    elif direction == RIGHT:      
      self.movement[0] = self.SPEED
    
  def StopMoving(self):
    if self.state != JUMP:
      self.state = STAND
    self.movement[0] = 0
    
  def StopJumping(self):
    if self.state == JUMP and self.movement[1] < 0:
      self.movement[1] = 0
  
  def Jump(self):
    if self.state != JUMP:
      self.movement[1] = self.movement[1] - self.JUMP
    self.state = JUMP
    
  def Supported(self):
    """The character is standing on something solid. This is the only way to leave JUMP state."""
    self.state = STAND
    self.movement[1] = 0
    
  def Gravity(self):
    """Decreases the character's upward momentum.
    
    This should only be called when the character is in the air (in JUMP state).
    """
    assert self.state == JUMP
    if self.movement[1] < self.TERMINAL_VELOCITY:
      self.movement[1] = min(self.movement[1] + self.GRAVITY, self.TERMINAL_VELOCITY)

  # TODO: handle falling and landing

  def update(self):
    new_position = self.rect.move(self.movement)
    # TODO: check for obstacles and stuff, besides just the edge of the environment.
    if not self.environment.contains(new_position):
      if new_position.top < self.environment.top:
        new_position.top = self.environment.top
      if new_position.bottom > self.environment.bottom:
        new_position.bottom = self.environment.bottom
      if new_position.left < self.environment.left:
        new_position.left = self.environment.left
      if new_position.right > self.environment.right:
        new_position.right = self.environment.right

    self.rect = new_position

    if self.rect.bottom >= self.environment.bottom:
      self.Supported()
    else:
      self.Gravity()
