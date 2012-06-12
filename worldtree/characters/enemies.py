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
  
  def __init__(self):
    self.hp = 50
    self.harmful = True
    # TODO: assign self.image to something
    
  def GetMove(self):
    """Get the movement vector for the badger."""
    