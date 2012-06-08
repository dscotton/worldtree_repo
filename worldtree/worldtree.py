"""
Main executable for the World Tree game.

Written for NaGaDeMo 2012 - http://nagademo.com/

Created on June 2, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import sys

import pygame
pygame.init()

from characters import hero
import environment
import map_data
from game_constants import *

def RunGame():
  pygame.display.set_caption(GAME_NAME)
  screen = pygame.display.set_mode(SCREEN_SIZE)
  screen.fill(BLACK)
  clock = pygame.time.Clock()

  # TODO: handle initial map setup more intelligently.
  env = environment.Environment(map_data.map_data['Map1'])
  font = pygame.font.Font(os.path.join('media', 'font', 'PressStart2P.ttf'), 24)
  text = font.render("Level 1", False, WHITE)
  text_box = text.get_rect()
  text_box.top = 10
  text_box.left = 10
  screen.blit(text, text_box)

  screen.blit(env.GetImage(), MAP_POSITION)
  # TODO: System for figuring out initial position on the map.
  player = hero.Hero(env, position=(64, SCREEN_HEIGHT - MAP_HEIGHT))
  # player.rect.bottom = SCREEN_HEIGHT - environment.TILE_HEIGHT
  player_group = pygame.sprite.RenderUpdates(player)
  
  pygame.display.flip()
  pygame.mixer.music.load(os.path.join('media', 'music', 'photosynthesis_wip.ogg'))
  pygame.mixer.music.play(-1)
  while pygame.QUIT not in (event.type for event in pygame.event.get()):
    refresh_map = env.dirty
    clock.tick(60)
    screen.fill(BLACK)
    screen.blit(env.GetImage(), MAP_POSITION)
    dirty_rects = []
    
    player.HandleInput()
    player_group.update()
    dirty_rects = player_group.draw(screen)
    if refresh_map:
      # TODO: Get the dirty rect animation working correctly.
      pygame.display.update(pygame.Rect(MAP_POSITION[0], MAP_POSITION[1], MAP_WIDTH, MAP_HEIGHT))
    else:
      for rect in dirty_rects:
        # For some reason the returned dirty_rects doesn't draw the entire sprite for the
        # main character when moving.
        rect.top -= 3
        rect.left -= 3
        rect.width += 6
        rect.height += 6
      pygame.display.update(dirty_rects)
    
  sys.exit()

if __name__ == '__main__':
  # Set up dir correctly - required for compiled .exe to work reliably
  os.chdir(os.path.dirname(sys.argv[0]))
  RunGame()