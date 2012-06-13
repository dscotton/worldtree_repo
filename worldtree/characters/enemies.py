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
  
  STARTING_HP = 50
  SPEED = 1
  GRAVITY = 2
  TERMINAL_VELOCITY = 2
  STARTING_MOVEMENT = [-SPEED, 0]
  HARMFUL = True
  IMAGE_FILE = 'badger.png'
  # TODO: assign self.image to something

  def GetMove(self):
    """Get the movement vector for the badger."""
    return self.WalkBackAndForth()