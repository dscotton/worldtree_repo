"""
This class represents the player character.

TODO(dscotton): Much of this code should probably be moved up a level to be shared with
enemies, but I'm not yet sure what's needed, so I'm putting it here for now.

Created on Jun 2, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import os

import pygame

import animation
import character
import controller
import enemies
from game_constants import LEFT
from game_constants import RIGHT
from game_constants import ATTACK
from game_constants import SHOOT
import game_constants
import projectile


class Hero(character.Character):
  """The player character.
  
  Attributes:
    image: Surface object that currently represents the character.  Should be changed
      to the appropriate value in the images dict as the character moves.
    environment: an Environment class representing the area the character is in.
    direction: int LEFT or RIGHT, the direction the character is currently facing.
    rect: pygame Rect object representing the sprite's position.
    movement: [x, y] pair representing the character's rate of movement.  (Positive numbers
      are right and down).
    state: (direction, action) tuple describing what the character is doing.
    last_state: (direction, action) tuple for the previous frame.
    max_jumps: int for the maximum number of jumps the player can make before touching the
      ground.
    remaining_jumps: int for the number of jumps the player can make right now without needing
      to land.
    attacking: int number of frames to continue attacking.
      
  Constants:
    SIZE: default (x, y) size of the character. Overridden by the actual size of the current image.
    COLOR: default (R, B, G) color of the character. Only relevant in error cases.
    DEFAULT_STATE: default (action, direction) of the character. Relevant for picking sprite.
    SPEED: Normal walking speed.
    JUMP: Jumping power.
    GRAVITY: Rate of downward acceleration while falling.
    TERMINAL_VELOCITY: Maximum rate at which you can fall.
  """

  STARTING_HP = 30
  DAMAGE = 2
  # Actual size will be determined by the current image surface, but this is a 
  # default and guideline.
  WIDTH = 72
  HEIGHT = 96
  SIZE = (WIDTH, HEIGHT)
  COLOR = (0x00, 0xFF, 0x66)
  ACCEL = 2
  SPEED = 5
  JUMP_FORCE = 10
  JUMP_DURATION = 22
  GRAVITY = 2
  TERMINAL_VELOCITY = 10
  INVULNERABILITY_FRAMES = 120
  ATTACK_DURATION = 30
  SHOOTING_COOLDOWN = 30
  IS_PLAYER = True
  HITBOX_LEFT_OFFSET = 5
  HITBOX_RIGHT_OFFSET = 19

  # Store surfaces in class variables so they're only loaded once.
  WALK_RIGHT_ANIMATION = None
  WALK_LEFT_ANIMATION = None
  STAND_RIGHT_IMAGE = None
  STAND_LEFT_IMAGE = None
  JUMP_RIGHT_IMAGE = None
  JUMP_LEFT_IMAGE = None
  FALL_RIGHT_IMAGE = None
  FALL_LEFT_IMAGE = None
  
  # Some sounds
  ATTACK_SOUND = pygame.mixer.Sound(os.path.join('media', 'sfx', 'attack.wav'))
  JUMP_SOUND = pygame.mixer.Sound(os.path.join('media', 'sfx', 'jump.wav'))
  DEATH_SOUND = pygame.mixer.Sound(os.path.join('media', 'music', 'game_over.ogg'))
    
  def __init__(self, environment, position=(0, 0)):
    character.Character.__init__(self, environment, position)
  
    # Load the sprite graphics once so we don't need to reload on the fly.
    # TODO: These ideally would be class constants, but because of the dependency on LoadImage
    # they can't be without ugly formatting.  See if it's possibly by moving LoadImage to
    # a higher level.

    if Hero.WALK_RIGHT_ANIMATION is None:
      self.InitImage()
    self.image = self.WALK_RIGHT_ANIMATION.NextFrame()
    self.jump_ready = True
    # This should start at 1 and be upgraded by an item.
    self.max_jumps = 1
    self.remaining_jumps = self.max_jumps
    self.attack_ready = True
    self.attacking = 0
    self.shooting_cooldown = 0
    self.ammo = 0
    self.max_ammo = 0
    self.last_state = self.state

  @property
  def state(self):
    return (self.action, self.direction, self.attacking)

  def Hitbox(self):
    """Gets the Map hitbox for the sprite, which is relative to the map rather than the screen.
    
    The Hitbox needs to be smaller than the sprite, partly because of weird PyGame behavior
    where a rect of width X and height y actually touches (x+1) * (y+1) pixels.
    """
    x, y = self.env.MapCoordinateForScreenPoint(self.rect.left, self.rect.top)
    if self.attacking:
      return pygame.Rect(x + 1, y + 1, self.rect.width - 2, self.rect.height - 2)
    elif self.direction == LEFT:
      return pygame.Rect(x + self.HITBOX_LEFT_OFFSET, y + 1, 47, self.rect.height - 2)
    else:
      return pygame.Rect(x + self.HITBOX_RIGHT_OFFSET, y + 1, 47, self.rect.height - 2)

  def Fallbox(self):
    """Gets the character's fallbox, that shows the area they're standing on."""
    x, y = self.env.MapCoordinateForScreenPoint(self.rect.left, self.rect.top)
    if self.direction == LEFT:
      fallbox = pygame.Rect(x + self.HITBOX_LEFT_OFFSET, y + 1, 47, self.rect.height - 2)
      fallbox.right = self.Hitbox().right
    else:
      fallbox = pygame.Rect(x + self.HITBOX_RIGHT_OFFSET, y + 1, 47, self.rect.height - 2)
    return fallbox

  def InitImage(self):
    # Walking animation
    walk_images = character.LoadImages('treeguywalk*.png', scaled=True,
                                       colorkey=game_constants.SPRITE_COLORKEY)
    self.WALK_RIGHT_ANIMATION = animation.Animation(walk_images)
    self.WALK_LEFT_ANIMATION = animation.Animation(
        [pygame.transform.flip(i, 1, 0) for i in walk_images])
    self.STAND_RIGHT_IMAGE = character.LoadImage('treeguyidle0000.png', scaled=True,
                                                colorkey=game_constants.SPRITE_COLORKEY)
    self.STAND_LEFT_IMAGE = pygame.transform.flip(self.STAND_RIGHT_IMAGE, 1, 0)
    self.JUMP_RIGHT_IMAGE = character.LoadImage('treeguyjump0000.png', scaled=True,
                                                colorkey=game_constants.SPRITE_COLORKEY)
    self.JUMP_LEFT_IMAGE = pygame.transform.flip(self.JUMP_RIGHT_IMAGE, 1, 0)
    self.FALL_RIGHT_IMAGE = character.LoadImage('treeguyfall0000.png', scaled=True,
                                                colorkey=game_constants.SPRITE_COLORKEY)
    self.FALL_LEFT_IMAGE = pygame.transform.flip(self.FALL_RIGHT_IMAGE, 1, 0)
    attack_images = character.LoadImages('treeguystrikefollow*.png', scaled=True,
                                         colorkey=game_constants.SPRITE_COLORKEY)
    self.ATTACK_RIGHT_ANIMATION = animation.Animation(attack_images, looping=False)
    self.ATTACK_LEFT_ANIMATION = animation.Animation(
        [pygame.transform.flip(i, 1, 0) for i in attack_images], looping=False)

  def ResetAnimations(self):
    """Reset the non-looping animations."""
    self.ATTACK_LEFT_ANIMATION.Reset()
    self.ATTACK_RIGHT_ANIMATION.Reset()

  def HandleInput(self):
    """Handles user input to move the character and change his action."""
    if self.attacking:
      self.StopMoving()

    actions = controller.GetInput()
    if ((LEFT in actions and RIGHT in actions) or
        (LEFT not in actions and RIGHT not in actions)):
      self.StopMoving()
    elif LEFT in actions and (self.vertical != character.GROUNDED or self.attacking == 0):
      self.Walk(LEFT)
    elif RIGHT in actions and (self.vertical != character.GROUNDED or self.attacking == 0):
      self.Walk(RIGHT)
    if controller.JUMP in actions:
      self.Jump()
      self.jump_ready = False
    else:
      self.StopUpwardMovement()
      self.jump_ready = True
    if ATTACK in actions:
      if self.attacking <= 0:
        self.Attack()
        self.attack_ready = False
    elif SHOOT in actions and self.attacking <= 0 and self.shooting_cooldown <= 0 and self.ammo > 0:
      self.Shoot()
    else:
      self.attack_ready = True

    if self.attacking:
      self.attacking -= 1
      if self.attacking == 0:
        self.ResetAnimations()

  
  def StopMoving(self):
    if self.movement[0] > 0:
      self.movement[0] = max(self.movement[0] - self.GRAVITY, 0)
    elif self.movement[0] < 0:
      self.movement[0] = min(self.movement[0] + self.GRAVITY, 0)

    if self.vertical != character.JUMP and self.attacking == 0 and self.movement[0] == 0:
      self.action = character.STAND
    
  def StopUpwardMovement(self):
    if self.vertical == character.JUMP:
      self.vertical = character.FALL
      self.jump_duration = 0
  
  def Jump(self):
    """Initiate a jump or continue jumping.
    
    The rules for jumping are:
    - While holding down the jump button you will continue a current jump though its duration,
      then fall.
    - If you release the jump button, you become jump_ready again, but can only initiate 
      a new jump if you have any remaining_jumps.
    - Once you touch the ground, your remaining_jumps are restored.
    """
    if self.jump_ready and self.remaining_jumps > 0:
      self.JUMP_SOUND.play()
      self.vertical = character.JUMP
      self.remaining_jumps -= 1
      self.jump_duration = self.JUMP_DURATION
      self.movement[1] = -self.JUMP_FORCE
    elif self.vertical == character.JUMP:
      self.jump_duration -= 1
      if self.jump_duration == 0:
        self.vertical = character.FALL
    
  def Supported(self):
    """The character is standing on something solid."""
    self.vertical = character.GROUNDED
    self.remaining_jumps = self.max_jumps
    self.movement[1] = min(0, self.movement[1])
    
  def Attack(self):
    """Initiate an attack action."""
    if self.attack_ready:
      self.ATTACK_SOUND.play()
      self.attacking = self.ATTACK_DURATION
    
  def Shoot(self):
    """Fire a projectile."""
    self.ammo -= 1
    self.shooting_cooldown = self.SHOOTING_COOLDOWN
    if self.direction == LEFT:
      direction = [-1, 0]
      position = (self.rect.left - 8, self.rect.centery - 32)
    else:
      direction = [1, 0]
      position = (self.rect.right, self.rect.centery - 32)
      
    bullet = projectile.SeedBullet(self.env, direction, position)
    self.env.hero_projectile_group.add(bullet)
    
  def SetCurrentImage(self):
    """Sets the image to the appropriate one for the current action, if it exists.
    
    Specialized behavior for the player character - changes the sprite width to match the image,
    and right-aligns the sprite when facing left rather than using default left-alignment.
    """
    # A dict of state->image seemed nice and pythonic at first, but it's not nearly as good
    # at handling cases where we don't want a different image for every possible state. This
    # way we can only apply animation logic when there are multiple frames to animate.

    # Save right edge to maintain right-alignment if width changes and sprite is facing left.
    # TODO: This could allow the character to penetrate walls with an attack, and if moving
    # possibly end up inside the wall afterward?  Investigate whether it's a problem.
    right = self.rect.right
    if LEFT == self.direction:
      if self.attacking > 0:
        self.image = self.ATTACK_LEFT_ANIMATION.NextFrame()
      elif character.JUMP == self.vertical:
        self.image = self.JUMP_LEFT_IMAGE
      elif character.FALL == self.vertical:
        self.image = self.FALL_LEFT_IMAGE
      elif character.WALK == self.action:
        self.image = self.WALK_LEFT_ANIMATION.NextFrame()
      else:
        self.image = self.STAND_LEFT_IMAGE
    elif RIGHT == self.direction:
      if self.attacking > 0:
        self.image = self.ATTACK_RIGHT_ANIMATION.NextFrame()
      elif character.JUMP == self.vertical:
        self.image = self.JUMP_RIGHT_IMAGE
      elif character.FALL == self.vertical:
        self.image = self.FALL_RIGHT_IMAGE
      elif character.WALK == self.action:
        self.image = self.WALK_RIGHT_ANIMATION.NextFrame()
      else:
        self.image = self.STAND_RIGHT_IMAGE
    self.FlickerIfInvulnerable()
    
    # Account for position changes due to different size sprites / different hitbox alignment.
    self.rect.width = self.image.get_width()
    if self.direction == LEFT:
      self.rect.right = right
    if self.direction != self.last_state[1]:
      if self.direction == LEFT:
        self.rect.left += (self.HITBOX_RIGHT_OFFSET - self.HITBOX_LEFT_OFFSET)
        if self.last_state[2] and not self.state[2]:
          # Was attacking last frame, now we aren't
          self.rect.left -= 72
      else:
        self.rect.left -= (self.HITBOX_RIGHT_OFFSET - self.HITBOX_LEFT_OFFSET)
        if self.last_state[2] and not self.state[2]:
          # Was attacking last frame, now we aren't
          self.rect.left += 72

  def CollideWith(self, enemy):
    """Handle what happens when the player collides with an enemy."""
    if self.attacking > 0:
      if not enemy.invulnerable:
        enemy.TakeHit(self.DAMAGE)
        enemy.CollisionPushback(self)
    else:
      if self.invulnerable == 0 or (type(enemy) == enemies.BoomBug and enemy.exploding):
        self.CollisionPushback(enemy)
      if self.invulnerable == 0 and enemy.invulnerable == 0:
        self.TakeHit(enemy.DAMAGE)
        # Calculate pushback

  def ChangeRooms(self, env, position):
    """Move this sprite to a new environment."""
    self.env = env
    map_rect = self.env.RectForTile(*position)
    left, top = self.env.ScreenCoordinateForMapPoint(map_rect.left, map_rect.top)
    if self.direction == LEFT:
      left -= self.HITBOX_LEFT_OFFSET
    else:
      left -= self.HITBOX_RIGHT_OFFSET
    self.rect = pygame.Rect((left, top),
                            (self.WIDTH, self.HEIGHT))

  def update(self):
    self.SetCurrentImage()
    new_rect = self.env.AttemptMove(self, self.movement)
    scroll_rect = self.env.ScreenRectForMapRect(self.Fallbox())
    scroll_vector = self.env.Scroll(scroll_rect)
    new_rect = new_rect.move(scroll_vector)
    self.rect = new_rect
    if (self.env.IsRectSupported(self.Fallbox())
        or self.env.IsRectSupported(self.Fallbox().move(self.movement[0], 0))):
      # Second part needed for wall jumping
      self.Supported()
    elif self.vertical != character.JUMP:
      # If not supported and not jumping, handle FALL status.
      self.vertical = character.FALL
      self.Gravity()
    if self.invulnerable > 0:
      self.invulnerable -= 1
    # TODO: Consider checking a clock and storing the last value so this doesn't have to be
    # ticked down every frame.
    self.shooting_cooldown -= 1
    self.last_state = self.state

  def Die(self):
    """Game over, man."""
    self.invulnerable = 2**31
    self.env.dying_animation_group.add(character.Dying(self.rect, player=True,
                                                       sound=self.DEATH_SOUND))
    self.kill()
    