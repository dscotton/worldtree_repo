"""
Abstract class representing any game character.

Created on Jun 11, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os
import random

import pygame

import animation
import character
import game_constants
import powerup
import projectile

class Beaver(character.Character):
  """Class for the notorious primary foe, the Beaver."""
  
  STARTING_HP = 10
  SPEED = 1
  GRAVITY = 2
  TERMINAL_VELOCITY = 2
  STARTING_MOVEMENT = [-SPEED, 0]
  DAMAGE = 1
  IMAGES = None
  ITEM_DROPS = [powerup.HealthRestore, powerup.AmmoRestore]
  DROP_PROBABILITY = 20
  WIDTH = 96
  HEIGHT = 60

  def GetMove(self):
    """Get the movement vector for the Beaver."""
    return self.WalkBackAndForth()
  
  def InitImage(self):
    if Beaver.IMAGES is None:
      Beaver.IMAGES = character.LoadImages('beaver1*.png', scaled=True,
                                           colorkey=game_constants.SPRITE_COLORKEY)
    self.walk_left_animation = animation.Animation(Beaver.IMAGES, framedelay=3)
    self.walk_right_animation = animation.Animation(
        [pygame.transform.flip(i, 1, 0) for i in Beaver.IMAGES], framedelay=3)
    self.SetCurrentImage()

  def SetCurrentImage(self):
    if self.movement[0] <= 0:
      self.image = self.walk_left_animation.NextFrame()
    else:
      self.image = self.walk_right_animation.NextFrame()


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
  FLY_LEFT_IMAGES = None
  
  def __init__(self, environment, position):
    self.vector = [-1, 0]
    character.Character.__init__(self, environment, position)
    self.move_frames = self.HORIZONTAL_MOVE_TIME
    self.rest_frames = 0
    # direction has a different meaning than for other characters.
    self.movement = [-self.SPEED, 0]

  def InitImage(self):
    if Dragonfly.FLY_LEFT_IMAGES is None:
      Dragonfly.FLY_LEFT_IMAGES = character.LoadImages('dragonfly*.png', scaled=True,
                                                       colorkey=game_constants.SPRITE_COLORKEY)
    self.fly_left_animation = animation.Animation(Dragonfly.FLY_LEFT_IMAGES)
    self.fly_right_animation = animation.Animation(
        [pygame.transform.flip(i, 1, 0) for i in Dragonfly.FLY_LEFT_IMAGES])
    self.SetCurrentImage()

  def SetCurrentImage(self):
    if self.vector[0] + self.vector[1] < 0:
      self.image = self.fly_left_animation.NextFrame()
    else:
      self.image = self.fly_right_animation.NextFrame()

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
    self.FlickerIfInvulnerable()
    if self.invulnerable > 0:
      self.invulnerable -= 1

class BoomBug(character.Character):
  """Enemy that explodes when the player comes near enough."""
  
  STARTING_HP = 4
  SPEED = 1
  GRAVITY = 2
  TERMINAL_VELOCITY = 2
  STARTING_MOVEMENT = [-SPEED, 0]
  DAMAGE = 1
  TRIGGER_RADIUS = 160
  EXPLODING_DAMAGE = 5
  EXPLODING_PUSHBACK = 48
  EXPLODING_DELAY = 90
  EXPLODING_FRAMES = 40
  WALKING_LEFT_IMAGES = None
  TRIGGERED_IMAGES = None
  EXPLODING_IMAGES = None
  
  DEATH_SOUND = pygame.mixer.Sound(os.path.join('media', 'sfx', 'silence.wav'))
  EXPLOSION_SOUND = pygame.mixer.Sound(os.path.join('media', 'sfx', 'explode.wav'))

  def __init__(self, environment, position):
    self.triggered = 0
    self.exploding = 0
    character.Character.__init__(self, environment, position)

  def InitImage(self):
    if BoomBug.WALKING_LEFT_IMAGES is None:
      BoomBug.WALKING_LEFT_IMAGES = character.LoadImages('bombug*.png', scaled=True,
                                                         colorkey=game_constants.SPRITE_COLORKEY)
    self.walk_left_animation = animation.Animation(BoomBug.WALKING_LEFT_IMAGES)
    self.walk_right_animation = animation.Animation(
        [pygame.transform.flip(i, 1, 0) for i in BoomBug.WALKING_LEFT_IMAGES])
    if BoomBug.TRIGGERED_IMAGES is None:
      BoomBug.TRIGGERED_IMAGES = character.LoadImages('bombexplosionleadup*.png', scaled=True,
                                                      colorkey=game_constants.SPRITE_COLORKEY)
    self.triggered_left_animation = animation.Animation(
        BoomBug.TRIGGERED_IMAGES, framedelay=6, looping=False)
    self.triggered_right_animation = animation.Animation(
        [pygame.transform.flip(i, 1, 0) for i in BoomBug.TRIGGERED_IMAGES],
        framedelay=6, looping=False)
    if BoomBug.EXPLODING_IMAGES is None:
      BoomBug.EXPLODING_IMAGES = character.LoadImages('bombexplode*.png', scaled=True,
                                                      colorkey=game_constants.SPRITE_COLORKEY)
    self.exploding_animation = animation.Animation(BoomBug.EXPLODING_IMAGES, framedelay=4, 
                                                   looping=False)
    
    self.SetCurrentImage()

  def SetCurrentImage(self):
    if self.exploding > 0:
      self.image = self.exploding_animation.NextFrame()
    elif self.direction == character.LEFT:
      if self.triggered > 0:
        self.image = self.triggered_left_animation.NextFrame()
      else:
        self.image = self.walk_left_animation.NextFrame()
    else:
      if self.triggered > 0:
        self.image = self.triggered_right_animation.NextFrame()
      else:
        self.image = self.walk_right_animation.NextFrame()

#  def Hitbox(self):
#    if self.exploding == 0:
#      return character.Character.Hitbox(self)
#    else:
      

  def SenseAndReturnHitbox(self, player):
    """Trigger an explosion if the player is close to the bug."""
    # TODO: See if this is too slow.
    if not (self.exploding or self.triggered):
      distance = self.GetDistance(player)
      if distance < self.TRIGGER_RADIUS:
        self.Trigger()
    return self.Hitbox()

  def GetMove(self):
    return self.WalkBackAndForth()

  def Trigger(self):
    self.triggered = self.EXPLODING_DELAY
    self.movement = [0, 0]

  def Explode(self):
    # TODO: Increase the effective size to the explosion radius.
    self.EXPLOSION_SOUND.play()
    midbottom = self.rect.midbottom
    self.rect.width, self.rect.height = self.EXPLODING_IMAGES[0].get_size()
    self.rect.midbottom = midbottom
    self.exploding = self.EXPLODING_FRAMES
    self.exploding_animation.Reset()
    self.DAMAGE = self.EXPLODING_DAMAGE
    self.PUSHBACK = self.EXPLODING_PUSHBACK

  def update(self):
    if self.triggered > 0:
      self.triggered -= 1
      if self.triggered == 0:
        self.Explode()
    elif self.exploding > 0:
      self.exploding -= 1
      if self.exploding == 0:
        self.env.dirty = True
        self.Die()
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
  """Enemy that shoots projectiles and targets the player if he is nearby."""
  
  WIDTH = 48
  HEIGHT = 96
  STARTING_HP = 5
  SPEED = 0
  GRAVITY = 2
  TERMINAL_VELOCITY = 2
  MOVEMENT = [0, 0]
  DAMAGE = 1
  SENSE_RADIUS = 480
  SHOOTING_COOLDOWN = 90
  IMAGES = None

  def __init__(self, environment, position):
    self.shooting_cooldown = 0
    character.Character.__init__(self, environment, position)
    self.aim = [0, -1]
    self.movement = self.MOVEMENT

  def InitImage(self):
    if Shooter.IMAGES is None:
      Shooter.IMAGES = character.LoadImages('mush*.png', scaled=True,
                                            colorkey=game_constants.SPRITE_COLORKEY)
    self.shoot_animation = animation.Animation(Shooter.IMAGES, framedelay=4)
    self.SetCurrentImage()

  def SetCurrentImage(self):
    if self.shooting_cooldown > 12:
      self.image = self.IMAGES[0]
    else:
      self.image = self.shoot_animation.NextFrame()

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
    self.env.enemy_projectile_group.add(bullet)

  def update(self):
    if self.shooting_cooldown > 0:
      self.shooting_cooldown -= 1
    else:
      self.Shoot()
      self.shoot_animation.Reset()
    self.SetCurrentImage()
    if self.invulnerable > 0:
      self.invulnerable -= 1
    self.last_state = self.state


class PipeBug(character.Character):
  """Enemy that flies up until level with the player, then left or right."""
  
  WIDTH = 48
  HEIGHT = 48
  STARTING_HP = 2
  SPEED = 8
  GRAVITY = 0
  DAMAGE = 1
  IMAGES = None

  def __init__(self, environment, position):
    character.Character.__init__(self, environment, position)
    self.movement = [0, -self.SPEED]
    self.turned = False

  def InitImage(self):
    if PipeBug.IMAGES is None:
      PipeBug.IMAGES = character.LoadImages('pipebee*.png', scaled=True,
                                            colorkey=game_constants.SPRITE_COLORKEY)
    self.left_animation = animation.Animation(PipeBug.IMAGES)
    self.right_animation = animation.Animation(
        [pygame.transform.flip(i, 1, 0) for i in PipeBug.IMAGES])
    self.SetCurrentImage()

  def SetCurrentImage(self):
    if self.movement[0] > 0:
      self.image = self.right_animation.NextFrame()
    else:
      self.image = self.left_animation.NextFrame()

  def SenseAndReturnHitbox(self, player):
    """Trigger an explosion if the player is close to the bug."""
    if not self.turned and self.rect.top < player.rect.top:
      self.turned = True
      if self.rect.centerx < player.rect.centerx:
        self.movement = [self.SPEED, 0]
      else:
        self.movement = [-self.SPEED, 0]

    return self.Hitbox()

  def GetMove(self):
    return self.movement

  def update(self):
    if not self.env.IsMoveLegal(self, self.GetMove()):
      self.kill()
    else:
      self.rect = self.rect.move(self.GetMove())
      self.SetCurrentImage()
      if self.invulnerable > 0:
        self.invulnerable -= 1

class BugPipe(character.Character):
  """Enemy that spawns a stream of PipeBugs."""
  
  WIDTH = 48
  HEIGHT = 48
  STARTING_HP = 1
  SPEED = 0
  GRAVITY = 0
  DAMAGE = 0
  IMAGE = None
  IMAGE_FILE = 'transparent.png'
  SPAWNING_COOLDOWN = 120
  
  def __init__(self, environment, position):
    character.Character.__init__(self, environment, position)
    self.spawning_cooldown = self.SPAWNING_COOLDOWN
    self.movement = [0, 0]
    self.invulnerable = 2**31

  def InitImage(self):
    if BugPipe.IMAGE is None:
      BugPipe.IMAGE = character.LoadImage(self.IMAGE_FILE, scaled=True, 
                                          colorkey=game_constants.SPRITE_COLORKEY)
    self.image = self.IMAGE

  def Hitbox(self):
    """Can't be hit."""
    return pygame.Rect(0, 0, 0, 0)
  
  def GetMove(self):
    return self.movement

  def SpawnBug(self):
    print 'spawning a bug!'
    self.spawning_cooldown = self.SPAWNING_COOLDOWN
    map_coordinate = self.env.MapCoordinateForScreenPoint(self.rect.centerx, self.rect.top-1)
    new_bug = PipeBug(self.env, self.env.TileIndexForPoint(*map_coordinate))
    self.env.enemy_group.add(new_bug)

  def update(self):
    if not self.env.IsScreenCoordinateVisible(*self.rect.midtop):
      return
    if self.spawning_cooldown > 0:
      self.spawning_cooldown -= 1
    else:
      self.SpawnBug()


class Batzor(character.Character):
  """Class for the Batzor enemy."""
  
  STARTING_HP = 3
  SPEED = 5  # Note: this speed applies along each axis.  Not Pythagorean!
  GRAVITY = 0
  STARTING_MOVEMENT = [0, 0]
  DAMAGE = 1
  REST_TIME = 20
  VARIABLE_REST = 120  # Random amount in this interval
  MOVE_TIME = 40
  IMAGES = None
  
  def __init__(self, environment, position):
    self.vector = [1, 1]
    character.Character.__init__(self, environment, position)
    self.move_frames = self.MOVE_TIME
    self.rest_frames = 0
    # direction has a different meaning than for other characters.
    self.movement = [-self.SPEED, 0]

  def InitImage(self):
    if Batzor.IMAGES is None:
      Batzor.IMAGES = character.LoadImages('batzor1*.png', scaled=True,
                                           colorkey=game_constants.SPRITE_COLORKEY)
    self.animation = animation.Animation(Batzor.IMAGES)
    self.SetCurrentImage()

  def Hitbox(self):
    x, y = self.env.MapCoordinateForScreenPoint(self.rect.left, self.rect.top)
    return pygame.Rect(x + 1, y + 1, self.rect.width - 2, self.rect.height - 24)

  def SetCurrentImage(self):
    self.image = self.animation.NextFrame()

  def GetMove(self):
    """Fly around diagonally."""
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
        self.move_frames = self.MOVE_TIME

    return self.movement

  def update(self):
    """Fliers have simpler update routines because we don't worry about gravity."""
    new_rect = self.env.AttemptMove(self, self.GetMove())
    self.rect = new_rect
    self.SetCurrentImage()
    self.FlickerIfInvulnerable()
    if self.invulnerable > 0:
      self.invulnerable -= 1


class Slug(character.Character):
  """Class for the notorious primary foe, the Slug."""
  
  STARTING_HP = 6
  SPEED = 1
  GRAVITY = 0
  TERMINAL_VELOCITY = 0
  STARTING_MOVEMENT = [-SPEED, 0]
  DAMAGE = 3
  IMAGES = None
  ITEM_DROPS = [powerup.HealthRestore, powerup.AmmoRestore]
  DROP_PROBABILITY = 30
  WIDTH = 96
  HEIGHT = 48
  REST_TIME = 20
  MOVE_TIME = 40
  
  def InitImage(self):
    if Slug.MOVE_LEFT_IMAGES is None:
      Slug.IDLE_LEFT_IMAGES = character.LoadImages('slug000*.png', scaled=True,
                                                   colorkey=game_constants.SPRITE_COLORKEY)
      Slug.MOVE_LEFT_IMAGES = character.LoadImages('slug001*.png', scaled=True,
                                                   colorkey=game_constants.SPRITE_COLORKEY)
      Slug.MOVE_LEFT_IMAGES.extend(character.LoadImages('slug002*.png', scaled=True,
                                                        colorkey=game_constants.SPRITE_COLORKEY))
      Slug.IDLE_RIGHT_IMAGES = [pygame.transform.flip(i, 1, 0) for i in Slug.IDLE_LEFT_IMAGES]
      Slug.MOVE_RIGHT_IMAGES = [pygame.transform.flip(i, 1, 0) for i in Slug.MOVE_LEFT_IMAGES]
      # TODO: Cache rotated images here if rotating on the fly proves to be too slow.
    self.walk_left_animation = animation.Animation(Slug.MOVE_LEFT_IMAGES, framedelay=3)
    self.idle_left_animation = animation.Animation(Slug.IDLE_LEFT_IMAGES, framedelay=3)
    self.walk_right_animation = animation.Animation(Slug.MOVE_RIGHT_IMAGES, framedelay=3)
    self.idle_right_animation = animation.Animation(Slug.IDLE_RIGHT_IMAGES, framedelay=3)
    self.SetCurrentImage()

  def SetCurrentImage(self):
    if self.movement[0] <= 0:
      self.image = self.walk_left_animation.NextFrame()
    else:
      self.image = self.walk_right_animation.NextFrame()

  def GetMove(self):
    """Inch along the wall."""
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
        self.move_frames = self.MOVE_TIME
