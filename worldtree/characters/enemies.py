"""
Abstract class representing any game character.

Created on Jun 11, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import random

import pygame

import animation
import character
import game_constants
import powerup
import projectile

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
  ITEM_DROPS = [powerup.HealthRestore, powerup.AmmoRestore]
  DROP_PROBABILITY = 20

  def GetMove(self):
    """Get the movement vector for the badger."""
    return self.WalkBackAndForth()
  

class Dragonfly(character.Character):
  """Class for the dragonfly enemy."""
  
  STARTING_HP = 2
  SPEED = 16
  GRAVITY = 0
  STARTING_MOVEMENT = [0, 0]
  DAMAGE = 1
  REST_TIME = 40
  VARIABLE_REST = 20  # Random amount in this interval
  HORIZONTAL_MOVE_TIME = 15
  VERTICAL_MOVE_TIME = 9
  ITEM_DROPS = [powerup.HealthRestore, powerup.AmmoRestore]
  DROP_PROBABILITY = 20
  
  def __init__(self, environment, position):
    self.vector = [-1, 0]
    character.Character.__init__(self, environment, position)
    self.move_frames = self.HORIZONTAL_MOVE_TIME
    self.rest_frames = 0
    # direction has a different meaning than for other characters.
    self.movement = [-self.SPEED, 0]

  def InitImage(self):
    fly_images = character.LoadImages('dragonfly*.png', scaled=True,
                                      colorkey=game_constants.SPRITE_COLORKEY)
    self.FLY_LEFT_ANIMATION = animation.Animation(fly_images)
    self.FLY_RIGHT_ANIMATION = animation.Animation(
        [pygame.transform.flip(i, 1, 0) for i in fly_images])
    self.SetCurrentImage()

  def SetCurrentImage(self):
    if self.vector[0] + self.vector[1] < 0:
      self.image = self.FLY_LEFT_ANIMATION.NextFrame()
    else:
      self.image = self.FLY_RIGHT_ANIMATION.NextFrame()

  def GetMove(self):
    """Dart around.  Alternatingly hover and move."""
    if self.move_frames > 0:
      self.movement = [i * self.SPEED for i in self.vector]
      self.move_frames -= 1
      if self.move_frames == 0:
        self.rest_frames = self.REST_TIME + random.randint(0, self.VARIABLE_REST)
    else:
      self.movement = [0, 0]
      self.rest_frames -= 1
      if self.rest_frames == 0:
        self.vector = [-self.vector[1], self.vector[0]]
        if self.vector[0] != 0:
          self.move_frames = self.HORIZONTAL_MOVE_TIME
        else:
          self.move_frames = self.VERTICAL_MOVE_TIME

    return self.movement

  def update(self):
    """Fliers have simpler update routines because we don't worry about gravity."""
    new_rect = self.env.AttemptMove(self, self.GetMove())
    self.rect = new_rect
    self.SetCurrentImage()
    if self.invulnerable > 0:
      self.invulnerable -= 1

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
  EXPLODING_DELAY = 60
  EXPLODING_FRAMES = 10

  def __init__(self, environment, position):
    self.triggered = 0
    self.exploding = 0
    character.Character.__init__(self, environment, position)

  def InitImage(self):
    # Walking animation
    walk_images = character.LoadImages('bombug*.png', scaled=True,
                                       colorkey=game_constants.SPRITE_COLORKEY)
    self.WALK_LEFT_ANIMATION = animation.Animation(walk_images)
    self.WALK_RIGHT_ANIMATION = animation.Animation(
        [pygame.transform.flip(i, 1, 0) for i in walk_images])
    self.SetCurrentImage()

  def SetCurrentImage(self):
    if self.direction == character.LEFT:
      self.image = self.WALK_LEFT_ANIMATION.NextFrame()
    else:
      self.image = self.WALK_RIGHT_ANIMATION.NextFrame()
    
    if (self.triggered / 4) % 2:
      self.image.set_alpha(128)
    else:
      self.image.set_alpha(255)
    # TODO: Add exploding animation

  def SenseAndReturnHitbox(self, player):
    """Trigger an explosion if the player is close to the bug."""
    # TODO: See if this is too slow.
    if (not (self.exploding or self.triggered)
        and ((self.rect.centerx - player.rect.centerx) ** 2 
             + (self.rect.centery - player.rect.centerx) ** 2) ** 0.5 < self.TRIGGER_RADIUS):
      self.Trigger()
    return self.Hitbox()

  def GetMove(self):
    return self.WalkBackAndForth()

  def Trigger(self):
    self.triggered = self.EXPLODING_DELAY

  def Explode(self):
    # TODO: Increase the effective size to the explosion radius.
    self.exploding = self.EXPLODING_FRAMES
    self.DAMAGE = self.EXPLODING_DAMAGE
    self.PUSHBACK = self.EXPLODING_PUSHBACK

  def update(self):
    if self.triggered > 0:
      self.triggered -= 1
      if self.triggered == 0:
        self.Explode()
    if self.exploding > 0:
      self.exploding -= 1
      if self.exploding == 0:
        self.env.dirty = True
        self.kill()
    else:
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


class Shooter(character.Character):
  
  STARTING_HP = 5
  SPEED = 0
  GRAVITY = 2
  TERMINAL_VELOCITY = 2
  MOVEMENT = [0, 0]
  DAMAGE = 1
  SENSE_RADIUS = 240
  SHOOTING_COOLDOWN = 90

  def __init__(self, environment, position):
    character.Character.__init__(self, environment, position)
    self.aim = [0, -1]
    self.movement = self.MOVEMENT
    self.shooting_cooldown = 0

  def InitImage(self):
    # Need a shooting animation?
    # TODO: this
    walk_images = character.LoadImages('bombug*.png', scaled=True,
                                       colorkey=game_constants.SPRITE_COLORKEY)
    self.WALK_LEFT_ANIMATION = animation.Animation(walk_images)
    self.SetCurrentImage()

  def SetCurrentImage(self):
    pass

  def SenseAndReturnHitbox(self, player):
    """Trigger an explosion if the player is close to the bug."""
    if ((self.rect.centerx - player.rect.centerx) ** 2 
        + (self.rect.centery - player.rect.centerx) ** 2) ** 0.5 < self.SENSE_RADIUS:
      self.aim = [player.rect.centerx - self.rect.centerx, player.rect.centery - self.rect.centery]
    else:
      self.aim = [0, -1]
    return self.Hitbox()

  def GetMove(self):
    return self.movement

  def Shoot(self):
    self.shooting_cooldown = self.SHOOTING_COOLDOWN
    bullet = projectile.SporeCloud(self.env, self.aim, (self.rect.left, self.rect.centery))
    self.env.hero_projectile_group.add(bullet)

  def update(self):
    if self.shooting_cooldown > 0:
      self.shooting_cooldown -= 1
    else:
      self.Shoot()
    self.SetCurrentImage()
    if self.invulnerable > 0:
      self.invulnerable -= 1
    self.last_state = self.state
  