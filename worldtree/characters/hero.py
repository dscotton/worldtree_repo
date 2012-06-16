"""
This class represents the player character.

TODO(dscotton): Much of this code should probably be moved up a level to be shared with
enemies, but I'm not yet sure what's needed, so I'm putting it here for now.

Created on Jun 2, 2012

@author: dscotton@gmail.com (David Scotton)
"""

import math

import pygame

import animation
import character
import controller
import game_constants


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
    ongoing_action: int number of frames the last action continues, disallowing new input.
      
  Constants:
    SIZE: default (x, y) size of the character. Overridden by the actual size of the current image.
    COLOR: default (R, B, G) color of the character. Only relevant in error cases.
    DEFAULT_STATE: default (action, direction) of the character. Relevant for picking sprite.
    SPEED: Normal walking speed.
    JUMP: Jumping power.
    GRAVITY: Rate of downward acceleration while falling.
    TERMINAL_VELOCITY: Maximum rate at which you can fall.
  """

  STARTING_HP = 99
  DAMAGE = 10
  # Actual size will be determined by the current image surface, but this is a 
  # default and guideline.
  WIDTH = 72
  HEIGHT = 96
  SIZE = (WIDTH, HEIGHT)
  COLOR = (0x00, 0xFF, 0x66)
  ACCEL = 2
  SPEED = 4
  JUMP_FORCE = 30
  GRAVITY = 2
  TERMINAL_VELOCITY = 8
  INVULNERABILITY_FRAMES = 120
  ATTACK_DURATION = 30

  # Store surfaces in class variables so they're only loaded once.
  WALK_RIGHT_ANIMATION = None
  WALK_LEFT_ANIMATION = None
  JUMP_RIGHT_IMAGE = None
  JUMP_LEFT_IMAGE = None
    
  def __init__(self, environment, position=(0, 0)):
    character.Character.__init__(self, environment, position)
  
    # Load the sprite graphics once so we don't need to reload on the fly.
    # TODO: These ideally would be class constants, but because of the dependency on LoadImage
    # they can't be without ugly formatting.  See if it's possibly by moving LoadImage to
    # a higher level.

    if Hero.WALK_RIGHT_ANIMATION is None:
      self.InitImage()
    self.image = self.WALK_RIGHT_ANIMATION.NextFrame()
    self.last_state = (self.direction, self.action)
    self.animation_frame = 0
    self.ongoing_action = 0

  # TODO: customize the hitbox to better correspond with the part of the frame he actually takes up

  def InitImage(self):
    # Walking animation
    walk_images = character.LoadImages('treeguywalk*.png', scaled=True,
                                       colorkey=game_constants.SPRITE_COLORKEY)
    self.WALK_RIGHT_ANIMATION = animation.Animation(walk_images)
    self.WALK_LEFT_ANIMATION = animation.Animation(
        [pygame.transform.flip(i, 1, 0) for i in walk_images])
    self.JUMP_RIGHT_IMAGE = character.LoadImage('treeguyjump0000.png', scaled=True,
                                                colorkey=game_constants.SPRITE_COLORKEY)
    self.JUMP_LEFT_IMAGE = pygame.transform.flip(self.JUMP_RIGHT_IMAGE, 1, 0)
    attack_images = character.LoadImages('treeguystrike1*.png', scaled=True,
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
    if self.ongoing_action:
      # TODO: Make attack take a little longer, and not loop.
      self.StopMoving()
      self.ongoing_action -= 1
      if self.ongoing_action == 0:
        self.ResetAnimations()
      return

    actions = controller.GetInput()
    if ((controller.LEFT in actions and controller.RIGHT in actions) or
        (controller.LEFT not in actions and controller.RIGHT not in actions)):
      self.StopMoving()
    elif controller.LEFT in actions:
      self.Walk(character.LEFT)
    elif controller.RIGHT in actions:
      self.Walk(character.RIGHT)
    if controller.JUMP in actions:
      self.Jump()
    else:
      self.StopUpwardMovement()
    if controller.ATTACK in actions:
      self.Attack()
  
  def StopMoving(self):
    if self.movement[0] > 0:
      self.movement[0] = max(self.movement[0] - self.GRAVITY, 0)
    elif self.movement[0] < 0:
      self.movement[0] = min(self.movement[0] + self.GRAVITY, 0)

    if self.action != character.JUMP and self.ongoing_action == 0 and self.movement[0] == 0:
      self.action = character.STAND
    
  def StopUpwardMovement(self):
    if self.action == character.JUMP and self.movement[1] < 0:
      self.movement[1] += 1
  
  def Jump(self):
    if self.action != character.JUMP:
      self.movement[1] = self.movement[1] - self.JUMP_FORCE
    self.action = character.JUMP
    
  def Attack(self):
    """Initiate an attack action."""
    print 'attack!'
    self.ongoing_action = self.ATTACK_DURATION
    self.action = character.ATTACK
    
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
    if character.LEFT == self.direction:
      if character.JUMP == self.action:
        self.image = self.JUMP_LEFT_IMAGE
      elif character.ATTACK == self.action:
        self.image = self.ATTACK_LEFT_ANIMATION.NextFrame()
      else:
        self.image = self.WALK_LEFT_ANIMATION.NextFrame()
      self.rect.right = right
    elif character.RIGHT == self.direction:
      if character.JUMP == self.action:
        self.image = self.JUMP_RIGHT_IMAGE
      elif character.ATTACK == self.action:
        self.image = self.ATTACK_RIGHT_ANIMATION.NextFrame()
      else:
        self.image = self.WALK_RIGHT_ANIMATION.NextFrame()
    if self.invulnerable > 0 and self.invulnerable % 4 > 0:
      self.image.set_alpha(128)
    else:
      self.image.set_alpha(255)
    
    self.rect.width = self.image.get_width()
    if self.direction == character.LEFT:
      self.rect.right = right

  def CollideWith(self, enemy):
    """Handle what happens when the player collides with an enemy."""
    if self.action == controller.ATTACK:
      if not enemy.invulnerable:
        enemy.TakeHit(self.DAMAGE)
        enemy.CollisionPushback(self)
    elif self.invulnerable == 0:
      self.TakeHit(enemy.DAMAGE)
      # Calculate pushback
      self.CollisionPushback(enemy)
      print 'Player health: %s' % self.hp
      # TODO: Calculate pushback vector here and modify movement.  This also will mean making
      # lateral movement behave with inertia.

  def update(self):
    self.SetCurrentImage()
    new_rect = self.env.AttemptMove(self, self.movement)
    scroll_vector = self.env.Scroll(self.rect)
    new_rect = new_rect.move(scroll_vector)
    self.rect = new_rect
    if self.env.IsRectSupported(self.Hitbox()):
      self.Supported()
    else:
      # If the character actually is falling, set them in jump status.
      self.action = character.JUMP
      self.Gravity()
    if self.invulnerable > 0:
      self.invulnerable -= 1
    self.last_state = self.state
