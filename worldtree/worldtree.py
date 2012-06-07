"""
Main executable for the World Tree game.

Written for NaGaDeMo 2012 - http://nagademo.com/

Created on June 2, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os
import sys

import pygame
pygame.init()

from characters import hero
import environment
from game_constants import *

def RunGame():
  # root_path = os.getcwd()
  # media_path = os.path.join(root_path, 'media')
  # image_path = os.path.join(media_path, 'sprites')
  pygame.display.set_caption(GAME_NAME)
  screen = pygame.display.set_mode(SCREEN_SIZE)
  screen.fill(BG_COLOR)
  clock = pygame.time.Clock()

  fh = open(os.path.join(environment.MAPS_PATH, 'test.map'))
  env = environment.Environment(fh.read())
  screen.blit(env.GetImage(), MAP_POSITION)
  # TODO: System for figuring out initial position on the map.
  player = hero.Hero(env, position=(64, SCREEN_HEIGHT - MAP_HEIGHT))
  player.rect.bottom = SCREEN_HEIGHT - environment.TILE_HEIGHT
  player_group = pygame.sprite.RenderUpdates(player)
  
  pygame.display.flip()
  pygame.mixer.music.load(os.path.join('media', 'music', 'photosynthesis_wip.ogg'))
  pygame.mixer.music.play()
  while pygame.QUIT not in (event.type for event in pygame.event.get()):
    clock.tick(60)
    screen.fill(BG_COLOR)
    screen.blit(env.GetImage(), MAP_POSITION)
    dirty_rects = []
    
    # Input test
    '''
    font = pygame.font.Font(os.path.join('media', 'font', 'PressStart2P.ttf'), 36)
    status_text = ', '.join(str(x) for x in controller.GetInput())
    text = font.render(status_text, False, BLACK)
    text_box = text.get_rect()
    text_box.centerx = screen.get_rect().centerx
    text_box.centery = screen.get_rect().centery
    screen.blit(text, text_box)
    '''
    player.HandleInput()
    player_group.update()
    dirty_rects = player_group.draw(screen)
    pygame.display.update(dirty_rects)
    
  sys.exit()

if __name__ == '__main__':
  # Set up dir correctly - required for compiled .exe to work reliably
  os.chdir(os.path.dirname(sys.argv[0]))
  RunGame()