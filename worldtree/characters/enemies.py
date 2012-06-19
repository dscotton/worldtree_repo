"""
Abstract class representing any game character.

Created on Jun 11, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import pygame

import character
import game_constants

class Badger(character.Character):
  """Class for the notorious primary foe, the badger."""
  
  STARTING_HP = 4
  SPEED = 1
  GRAVITY = 2
  TERMINAL_VELOCITY = 2
  STARTING_MOVEMENT = [-SPEED, 0]
  DAMAGE = 1
  IMAGE_FILE = 'badger.png'
  # TODO: assign self.image to something

  def GetMove(self):
    """Get the movement vector for the badger."""
    return self.WalkBackAndForth()
  

class Dragonfly(character.Character):
  """Class for the dragonfly enemy."""
  
  STARTING_HP = 2
  SPEED = 8
  GRAVITY = 0
  STARTING_MOVEMENT = [0, 0]
  DAMAGE = 1
  
  def GetMove(self):
    """Dart around.  Alternatingly hover and move."""
    raise NotImplementedError('Still need to write this!')
  
  
class BombBug(character.Character):
  
  STARTING_HP = 4
  SPEED = 1
  GRAVITY = 2
  TERMINAL_VELOCITY = 2
  STARTING_MOVEMENT = [-SPEED, 0]
  DAMAGE = 1
  TRIGGER_RADIUS = 96
  EXPLODING_DAMAGE = 3
  EXPLODING_PUSHBACK = 48
  EXPLODING_FRAMES = 10
  IMAGE_FILE = 'badger.png'

  def __init__(self):
    self.exploding = 0

  def GetMove(self):
    return self.WalkBackAndForth()

  def Explode(self):
    if self.exploding == 0:
      # TODO: Increase the effective size to the explosion radius.
      self.exploding = self.EXPLODING_FRAMES
      self.DAMAGE = self.EXPLODING_DAMAGE
      self.PUSHBACK = self.EXPLODING_PUSHBACK

  def update(self):
    if self.exploding > 0:
      self.exploding -= 1
      if self.exploding == 0:
        self.env.dirty = True
        self.kill()
      
    new_rect = self.env.AttemptMove(self, self.GetMove())
    self.rect = new_rect
    if self.env.IsRectSupported(self.Hitbox()):
      self.Supported()
    else:
      self.Gravity()
    self.SetCurrentImage()
    if self.invulnerable > 0:
      self.invulnerable -= 1
    self.last_state = self.state
  