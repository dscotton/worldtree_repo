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
  """
  
  # Override these in child classes to change movement behavior.
  GRAVITY = 0
  TERMINAL_VELOCITY = 0
  SPEED = 0
  WIDTH = 32
  HEIGHT = 32
  SIZE = (WIDTH, HEIGHT)
  DEFAULT_STATE = (STAND, LEFT)

  def __init__(self):
    pygame.sprite.Sprite.__init__(self)
    self.hp = 1
    self.invulnerable = False
    self.solid = True
    self.harmful = False

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

  def GetMove(self):
    """Method for getting the unit's movement vector.
    
    Must be implemented by any subclass that doesn't also override update().
    """
    raise NotImplementedError()

  def WalkBackAndForth(self):
    """Get movement for walking back and forth on the current platform occupied."""

  def update(self):
    new_rect = self.environment.AttemptMove(self, self.GetMove())
    if self.environment.IsRectSupported(self.Hitbox()):
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
    