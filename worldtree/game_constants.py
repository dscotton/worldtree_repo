"""
Shared constants that many modules want to have access to.

Written for NaGaDeMo 2012 - http://nagademo.com/

Created on June 5, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os

GAME_NAME = 'World Tree'
SCREEN_WIDTH = 960
SCREEN_HEIGHT = 720
MAP_WIDTH = 960
MAP_HEIGHT = 640
MAP_POSITION = (0, SCREEN_HEIGHT - MAP_HEIGHT)
SCROLL_MARGIN = 160
SCREEN_SIZE = (SCREEN_WIDTH, SCREEN_HEIGHT)
MAP_SIZE = (SCREEN_WIDTH, MAP_HEIGHT)
BLACK = (0, 0, 0)
WHITE = (0xFF, 0xFF, 0xFF)
BG_COLOR = (0x44, 0xCC, 0xFF)
TILE_WIDTH = 32
TILE_HEIGHT = 32
TILE_SIZE = (TILE_WIDTH, TILE_HEIGHT)
TILE_DIR = os.path.join('media', 'tiles')
HORIZONTAL_TILE_COUNT = MAP_WIDTH / TILE_SIZE[0]
VERTICAL_TILE_COUNT = MAP_HEIGHT / TILE_SIZE[1]
