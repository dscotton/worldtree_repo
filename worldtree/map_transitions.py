"""
Data about transitions from the edge of one room to another.

Created on Jun 16, 2012

@author: dscotton@gmail.com (David Scotton)
"""

from controller import LEFT
from controller import RIGHT
from controller import UP
from controller import DOWN

class Transition(object):
  """Encapsulates the data describing a one-way transition between maps.
  
  All transitions take place along a single axis (i.e. left-right or up-down).  For simplicity
  the fields in this object are all relative to that axis, thus, first is the first x tile for
  an up-down transition, but the first y tile for a left-right transition.
  
  Attributes:
    direction: constant from controller, one of UP, DOWN, LEFT, RIGHT, the player is moving.
    first: int, tile index where this edge is first connected to another map.
    last: int, tile index where the edge is last connected to another map (inclusive).
    dest: str, the name of the destination room (one of the keys to map_data).
    offset: int, number of tiles offset the destination map is.  The player will appear at the
      tile given by current_position + offset.
  """

  def __init__(self, direction, first, last, destination, offset):
    self.direction = direction
    self.first = first
    self.last = last
    self.dest = destination
    self.offset = offset


transitions = {
  'Map1' : {
    LEFT : (Transition(LEFT, 0, 19, 'Map4', 0), 
            Transition(LEFT, 20, 39, 'Map2', -20)),
    RIGHT : (Transition(RIGHT, 0, 17, 'Map5', 17),
             Transition(RIGHT, 18, 39, 'Map11', -18),),
    UP : (Transition(UP, 30, 49, 'Map6', -30),),
    DOWN : (Transition(DOWN, 15, 49, 'Map3', -15),),
  },
  'Map2' : {
    LEFT : (Transition(LEFT, 0, 19, 'Map31', 0),),
    RIGHT : (Transition(RIGHT, 0, 19, 'Map1', 20),),
    UP : (Transition(UP, 0, 79, 'Map4', 0),),
  },
  'Map3' : {
    UP : (Transition(UP, 0, 34, 'Map1', 15),
          Transition(UP, 35, 64, 'Map11', -35),),
    LEFT : (Transition(LEFT, 34, 39, 'Map9', -34),),
    RIGHT : (Transition(RIGHT, 0, 39, 'Map30', 22),),
  },
  'Map4' : {
    RIGHT : (Transition(RIGHT, 0, 19, 'Map1', 0),),
    DOWN : (Transition(DOWN, 0, 79, 'Map2', 0),),
  },
  'Map5' : {
    LEFT : (Transition(LEFT, 27, 34, 'Map1', -17),
            Transition(LEFT, 0, 11, 'Map6', 3)),
    DOWN : (Transition(DOWN, 0, 34, 'Map11', 0),),
  },
  'Map6' : {
    RIGHT : (Transition(RIGHT, 3, 19, 'Map5', -3),),
    DOWN : (Transition(DOWN, 0, 19, 'Map1', 30),),
  },
  'Map7' : {
    RIGHT : (Transition(RIGHT, 0, 19, 'Map8', 13),),
    LEFT : (Transition(LEFT, 0, 39, 'Map24', 0),),
  },
  'Map8' : {
    RIGHT : (Transition(RIGHT, 0, 5, 'Map9', 94),),
    LEFT : (Transition(LEFT, 13, 32, 'Map7', -13),),
  },
  'Map9' : {
    RIGHT : (Transition(RIGHT, 0, 5, 'Map3', 34),),
    LEFT : (Transition(LEFT, 16, 35, 'Map17', -16),
            Transition(LEFT, 94, 99, 'Map8', -94),),
  },
  'Map10' : {
    LEFT : (Transition(LEFT, 0, 89, 'Map26', 0),),
    RIGHT : (Transition(RIGHT, 0, 9, 'Map12', 60),
             Transition(RIGHT, 56, 70, 'Map21', -56),
             Transition(RIGHT, 75, 89, 'Map25', -75),),
  },
  'Map11' : {
    LEFT : (Transition(LEFT, 0, 21, 'Map1', 18),),
    RIGHT : (Transition(RIGHT, 0, 21, 'Map30', 1),),
    UP : (Transition(UP, 0, 34, 'Map5', 0),),
    DOWN : (Transition(DOWN, 0, 29, 'Map3', 35),),
  },
  'Map12' : {
    LEFT : (Transition(LEFT, 0, 10, 'Map13', 19),
            Transition(LEFT, 60, 69, 'Map10', -60),),
    RIGHT : (Transition(RIGHT, 0, 69, 'Map14', 0),),
  },
  'Map13' : {
    RIGHT : (Transition(RIGHT, 0, 13, 'Map16', 6),
             Transition(RIGHT, 19, 29, 'Map12', -19),),
  },
  'Map14' : {
    LEFT : (Transition(LEFT, 0, 69, 'Map12', 0),),
    RIGHT : (Transition(RIGHT, 0, 69, 'Map18', 0),),
  },
  'Map16' : {
    RIGHT : (Transition(RIGHT, 0, 19, 'Map2', 0),),
    LEFT : (Transition(LEFT, 0, 19, 'Map13', -6),),
  },
  'Map17' : {
    LEFT : (Transition(LEFT, 0, 19, 'Map23', 0),),
    RIGHT : (Transition(RIGHT, 0, 19, 'Map9', 16),),
    UP : (Transition(UP, 0, 39, 'Map20', 0),),
  },
  'Map18' : {
    LEFT : (Transition(LEFT, 0, 69, 'Map14', 0),),
    RIGHT : (Transition(RIGHT, 0, 14, 'Map19', 0),
             Transition(RIGHT, 50, 69, 'Map23', -50),),
  },
  'Map19' : {
    LEFT : (Transition(LEFT, 0, 14, 'Map18', 0),),
  },
  'Map20' : {
    DOWN : (Transition(DOWN, 0, 39, 'Map17', 0),),
  },
  'Map21' : {
    LEFT : (Transition(LEFT, 0, 14, 'Map10', 56),),
    UP : (Transition(UP, 18, 54, 'Map22', -18),),
  },
  'Map22' : {
    DOWN : (Transition(DOWN, 0, 36, 'Map21', 18),),
  },
  'Map23' : {
    LEFT : (Transition(LEFT, 0, 19, 'Map18', 50),),
    RIGHT : (Transition(RIGHT, 0, 19, 'Map17', 0),),
  },
  'Map24' : {
    LEFT : (Transition(LEFT, 0, 11, 'Map25', 4),
            Transition(LEFT, 41, 60, 'Map28', -41),),
    RIGHT : (Transition(RIGHT, 0, 39, 'Map7', 0),),
  },
  'Map25' : {
    LEFT : (Transition(LEFT, 0, 15, 'Map10', 75),),
    RIGHT : (Transition(RIGHT, 4, 15, 'Map24', -4),),
  },
  'Map26' : {
    LEFT : (Transition(LEFT, 60, 74, 'Map29', -60),),
    RIGHT : (Transition(RIGHT, 0, 89, 'Map10', 0),
             Transition(RIGHT, 120, 139, 'Map27', -120),),
  },
  'Map27' : {
    LEFT : (Transition(LEFT, 0, 19, 'Map26', 120),),
    RIGHT : (Transition(RIGHT, 0, 19, 'Map28', 0),),
  },
  'Map28' : {
    LEFT : (Transition(LEFT, 0, 19, 'Map27', 0),),
    RIGHT : (Transition(RIGHT, 0, 19, 'Map24', 41),),
  },
  'Map29' : {
    RIGHT : (Transition(RIGHT, 0, 14, 'Map26', 60),),
  },
  'Map30' : {
    LEFT : (Transition(LEFT, 0, 21, 'Map11', 0),
            Transition(LEFT, 22, 61, 'Map3', -22),),
  },
  'Map31' : {
    LEFT : (Transition(LEFT, 0, 19, 'Map16', 0),),
  },
}