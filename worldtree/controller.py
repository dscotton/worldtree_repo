"""
Basic controller module interprets user input and returns it as signal list.

This provides a layer of abstraction between the hardware input and the game
signal (up, down, start, etc).  Initially it will only handle keyboard input
but if we have time we can add support for gamepads and/or configurable keys.

@author: dscotton@gmail.com (David Scotton)
"""

import pygame

# These constants represent the meaning of the user input to the game.
UP = 1
DOWN = 2
LEFT = 3
RIGHT = 4
JUMP = 5
ATTACK = 6
START = 7

ALL_ACTIONS = (UP, DOWN, LEFT, RIGHT, JUMP, ATTACK, START)

# This maps keys to actions.  Multiple keys can map to a single actions, and
# the default scheme is designed to allow directions to be done with arrows
# or WASD depending on preference.
# HOWEVER - a large keymap could potentially hurt performance since we iterate
# over it every frame.
KEY_MAP = {
  pygame.K_UP : UP,
  pygame.K_DOWN : DOWN,
  pygame.K_LEFT : LEFT,
  pygame.K_RIGHT : RIGHT,
  pygame.K_SPACE : JUMP,
  pygame.K_m : ATTACK,
  pygame.K_RETURN : START,
  pygame.K_w : UP,
  pygame.K_a : LEFT,
  pygame.K_s : DOWN,
  pygame.K_d : RIGHT
}

def GetInput():
  """Returns a list of the currently active active_keys signals."""
  active_keys = pygame.key.get_pressed()
  return [action for (key, action) in KEY_MAP.iteritems() if active_keys[key]]
  