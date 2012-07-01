"""
Class for displaying information at the top of the screen.

Created on June 20, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os

import pygame

import environment
import game_constants

REGION_NAMES = {
  'photosynthesis.ogg': 'High Branches',
  'foreboding_cave.ogg': 'Ruins of Asgard',
  'nighttime.ogg': 'Inside the Trunk',
  'ozor.ogg':  'Star Caves',
  'bongo_wip.ogg': "The Baron's Lair",
}

class Statusbar(object):
  """This class generates an image to display at the top of the screen."""
  
  def __init__(self, player):
    self.player = player
    self.image = pygame.surface.Surface((game_constants.SCREEN_WIDTH,
                                         game_constants.SCREEN_HEIGHT - game_constants.MAP_HEIGHT))
    self.font = pygame.font.Font(os.path.join('media', 'font', game_constants.FONT), 24)
    self.hp = 0
    self.max_hp = 0
    self.ammo = 0
    self.region = 0
    self.room = 0
    self.dirty = False
    
  def GetImage(self):
    self.image.fill(game_constants.BLACK)
    if self.hp != self.player.hp or self.max_hp != self.player.max_hp:
      self.hp = self.player.hp
      self.max_hp = self.player.max_hp
      self.dirty = True
    hp_text = self.font.render("Health: %s/%s" % (self.player.hp, self.player.max_hp),
                               False, game_constants.WHITE)
    hp_text_box = hp_text.get_rect()
    hp_text_box.top = 10
    hp_text_box.left = 10
    self.image.blit(hp_text, hp_text_box)

    if self.player.max_ammo > 0:
      if self.ammo != self.player.ammo:
        self.ammo = self.player.ammo
        self.dirty = True
      ammo_text = self.font.render("Seeds: %s/%s" % (self.player.ammo, self.player.max_ammo),
                                   False, game_constants.WHITE)
      ammo_text_rect = ammo_text.get_rect()
      ammo_text_rect.top = 45
      ammo_text_rect.left = 10
      self.image.blit(ammo_text, ammo_text_rect)

    if self.region != self.player.env.region:
      self.region = self.player.env.region
      self.dirty = True
    region_text = self.font.render(
        REGION_NAMES[environment.SONGS_BY_ROOM[self.region][self.player.env.name]],
        False, game_constants.WHITE)
    region_text_box = region_text.get_rect()
    region_text_box.top = 10
    region_text_box.right = game_constants.SCREEN_WIDTH - 10
    self.image.blit(region_text, region_text_box)

    room_number = self.player.env.name[3:]
    if room_number != self.room:
      self.room = room_number
      self.dirty = True
    room_text = self.font.render("Room %s" % room_number, False, game_constants.WHITE)
    room_text_box = room_text.get_rect()
    room_text_box.top = 45
    room_text_box.right = game_constants.SCREEN_WIDTH - 10
    self.image.blit(room_text, room_text_box)
    
    return self.image
