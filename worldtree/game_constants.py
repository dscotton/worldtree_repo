"""
Shared constants that many modules want to have access to.

Written for NaGaDeMo 2012 - http://nagademo.com/

Created on June 5, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os

# These are mostly graphics setup constants.
GAME_NAME = 'World Tree'
SCREEN_WIDTH = 960
SCREEN_HEIGHT = 720
MAP_WIDTH = 960
MAP_HEIGHT = 640
MAP_X = 0
MAP_Y = SCREEN_HEIGHT - MAP_HEIGHT
MAP_POSITION = (MAP_X, MAP_Y)
SCROLL_MARGIN = 160
SCREEN_SIZE = (SCREEN_WIDTH, SCREEN_HEIGHT)
MAP_SIZE = (SCREEN_WIDTH, MAP_HEIGHT)
BLACK = (0, 0, 0)
WHITE = (0xFF, 0xFF, 0xFF)
SPRITE_COLORKEY = (0xFF, 0, 0xFF)
BG_COLOR = (0x00, 0x93, 0xFF)
TILE_WIDTH = 48
TILE_HEIGHT = 48
TILE_SIZE = (TILE_WIDTH, TILE_HEIGHT)
TILE_DIR = os.path.join('media', 'tiles')
HORIZONTAL_TILE_COUNT = MAP_WIDTH / TILE_SIZE[0]
VERTICAL_TILE_COUNT = MAP_HEIGHT / TILE_SIZE[1]

# These constants represent the meaning of the user input to the game.
UP = 1
DOWN = 2
LEFT = 3
RIGHT = 4
JUMP = 5
ATTACK = 6
START = 7