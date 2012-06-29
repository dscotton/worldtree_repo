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
    region: int, the destination region that the dest room appears in.
    dest: str, the name of the destination room (one of the keys to map_data).
    offset: int, number of tiles offset the destination map is.  The player will appear at the
      tile given by current_position + offset.
  """

  def __init__(self, first, last, region, destination, offset):
    self.first = first
    self.last = last
    self.region = region
    self.dest = destination
    self.offset = offset


transitions = {
  1 : {
    'Map1' : {
      LEFT : (Transition(0, 19, 1, 'Map4', 0), 
              Transition(20, 39, 1, 'Map2', -20)),
      RIGHT : (Transition(0, 17, 1, 'Map5', 17),
               Transition(18, 39, 1, 'Map11', -18),),
      UP : (Transition(30, 49, 1, 'Map6', -30),),
      DOWN : (Transition(15, 49, 1, 'Map3', -15),),
    },
    'Map2' : {
      LEFT : (Transition(0, 19, 1, 'Map31', 0),),
      RIGHT : (Transition(0, 19, 1, 'Map1', 20),),
      UP : (Transition(0, 79, 1, 'Map4', 0),),
    },
    'Map3' : {
      UP : (Transition(0, 34, 1, 'Map1', 15),
            Transition(35, 64, 1, 'Map11', -35),),
      LEFT : (Transition(34, 39, 1, 'Map9', -34),),
      RIGHT : (Transition(0, 39, 1, 'Map30', 22),),
    },
    'Map4' : {
      RIGHT : (Transition(0, 19, 1, 'Map1', 0),),
      DOWN : (Transition(0, 79, 1, 'Map2', 0),),
    },
    'Map5' : {
      LEFT : (Transition(27, 34, 1, 'Map1', -17),
              Transition(0, 11, 1, 'Map6', 3)),
      RIGHT : (Transition(0, 34, 2, 'Map1', 14),),
      DOWN : (Transition(0, 34, 1, 'Map11', 0),
              Transition(35, 54, 1, 'Map30', -35),),
    },
    'Map6' : {
      RIGHT : (Transition(3, 19, 1, 'Map5', -3),),
      DOWN : (Transition(0, 19, 1, 'Map1', 30),),
    },
    'Map7' : {
      RIGHT : (Transition(0, 19, 1, 'Map8', 13),),
      LEFT : (Transition(0, 39, 1, 'Map24', 0),),
    },
    'Map8' : {
      RIGHT : (Transition(0, 5, 1, 'Map9', 94),),
      LEFT : (Transition(13, 32, 1, 'Map7', -13),),
    },
    'Map9' : {
      RIGHT : (Transition(0, 5, 1, 'Map3', 34),),
      LEFT : (Transition(16, 35, 1, 'Map17', -16),
              Transition(94, 99, 1, 'Map8', -94),),
    },
    'Map10' : {
      LEFT : (Transition(0, 89, 1, 'Map26', 0),),
      RIGHT : (Transition(0, 9, 1, 'Map12', 60),
               Transition(56, 70, 1, 'Map21', -56),
               Transition(75, 89, 1, 'Map25', -75),),
    },
    'Map11' : {
      LEFT : (Transition(0, 21, 1, 'Map1', 18),),
      RIGHT : (Transition(0, 21, 1, 'Map30', 1),),
      UP : (Transition(0, 34, 1, 'Map5', 0),),
      DOWN : (Transition(0, 29, 1, 'Map3', 35),),
    },
    'Map12' : {
      LEFT : (Transition(0, 10, 1, 'Map13', 19),
              Transition(60, 69, 1, 'Map10', -60),),
      RIGHT : (Transition(0, 69, 1, 'Map14', 0),),
    },
    'Map13' : {
      RIGHT : (Transition(0, 13, 1, 'Map16', 6),
               Transition(19, 29, 1, 'Map12', -19),),
    },
    'Map14' : {
      LEFT : (Transition(0, 69, 1, 'Map12', 0),),
      RIGHT : (Transition(0, 69, 1, 'Map18', 0),),
    },
    'Map16' : {
      RIGHT : (Transition(0, 19, 1, 'Map31', 0),),
      LEFT : (Transition(0, 19, 1, 'Map13', -6),),
    },
    'Map17' : {
      LEFT : (Transition(0, 19, 1, 'Map23', 0),),
      RIGHT : (Transition(0, 19, 1, 'Map9', 16),),
      UP : (Transition(0, 39, 1, 'Map20', 0),),
    },
    'Map18' : {
      LEFT : (Transition(0, 69, 1, 'Map14', 0),),
      RIGHT : (Transition(0, 14, 1, 'Map19', 0),
               Transition(50, 69, 1, 'Map23', -50),),
    },
    'Map19' : {
      LEFT : (Transition(0, 14, 1, 'Map18', 0),),
    },
    'Map20' : {
      DOWN : (Transition(0, 39, 1, 'Map17', 0),),
    },
    'Map21' : {
      LEFT : (Transition(0, 14, 1, 'Map10', 56),),
      UP : (Transition(18, 54, 1, 'Map22', -18),),
    },
    'Map22' : {
      DOWN : (Transition(0, 36, 1, 'Map21', 18),),
    },
    'Map23' : {
      LEFT : (Transition(0, 19, 1, 'Map18', 50),),
      RIGHT : (Transition(0, 19, 1, 'Map17', 0),),
    },
    'Map24' : {
      LEFT : (Transition(0, 11, 1, 'Map25', 4),
              Transition(41, 60, 1, 'Map28', -41),),
      RIGHT : (Transition(0, 39, 1, 'Map7', 0),),
    },
    'Map25' : {
      LEFT : (Transition(0, 15, 1, 'Map10', 75),),
      RIGHT : (Transition(4, 15, 1, 'Map24', -4),),
    },
    'Map26' : {
      LEFT : (Transition(60, 74, 1, 'Map29', -60),),
      RIGHT : (Transition(0, 89, 1, 'Map10', 0),
               Transition(120, 139, 1, 'Map27', -120),),
    },
    'Map27' : {
      LEFT : (Transition(0, 19, 1, 'Map26', 120),),
      RIGHT : (Transition(0, 19, 1, 'Map28', 0),),
    },
    'Map28' : {
      LEFT : (Transition(0, 19, 1, 'Map27', 0),),
      RIGHT : (Transition(0, 19, 1, 'Map24', 41),),
    },
    'Map29' : {
      RIGHT : (Transition(0, 14, 1, 'Map26', 60),),
    },
    'Map30' : {
      LEFT : (Transition(0, 21, 1, 'Map11', 0),
              Transition(22, 61, 1, 'Map3', -22),),
      RIGHT : (Transition(0, 61, 2, 'Map1', 48),),
      UP: (Transition(0, 19, 1, 'Map5', 35),),
    },
    'Map31' : {
      LEFT : (Transition(0, 19, 1, 'Map16', 0),),
      RIGHT : (Transition(0, 19, 1, 'Map2', 0),),
    },
  },
  2 : {
    'Map1' : {
      LEFT : (Transition(14, 47, 1, 'Map5', -14), 
              Transition(48, 109, 1, 'Map30', -48)),
      RIGHT : (Transition(0, 8, 2, 'Map2', 11),
               Transition(13, 27, 2, 'Map3', -13),
               Transition(28, 97, 2, 'Map4', -28),),
    },
    'Map2' : {
      LEFT : (Transition(11, 19, 2, 'Map1', -11),),
      RIGHT : (Transition(0, 19, 2, 'Map7', 42),),
    },
    'Map3' : {
      LEFT : (Transition(0, 14, 2, 'Map1', 13),),
      RIGHT : (Transition(0, 13, 2, 'Map7', 66),),
      DOWN : (Transition(0, 49, 2, 'Map4', 0),),
    },
    'Map4' : {
      LEFT : (Transition(0, 69, 2, 'Map1', 28),),
      RIGHT : (Transition(10, 69, 2, 'Map5', -10),),
      UP : (Transition(0, 49, 2, 'Map3', 0),)
    },
    'Map5' : {
      LEFT : (Transition(0, 59, 2, 'Map4', 10),),
      RIGHT : (Transition(0, 14, 2, 'Map6', 0),
               Transition(45, 59, 2, 'Map21', -45),),
    },
    'Map6' : {
      LEFT : (Transition(0, 14, 2, 'Map5', 0),),
    },
    'Map7' : {
      LEFT : (Transition(5, 19, 2, 'Map8', -5),
              Transition(42, 61, 2, 'Map2', -42),
              Transition(66, 79, 2, 'Map3', -66),),
      RIGHT : (Transition(0, 29, 2, 'Map9', 0),
               Transition(72, 79, 2, 'Map15', -72),),
    },
    'Map8' : {
      LEFT : (Transition(0, 14, 2, 'Map10', 35),),
      RIGHT : (Transition(0, 14, 2, 'Map7', 5),),
    },
    'Map9' : {
      LEFT : (Transition(0, 29, 2, 'Map7', 0),),
      RIGHT : (Transition(6, 20, 2, 'Map19', -6),),
      DOWN : (Transition(20, 59, 2, 'Map16', -20),),
    },
    'Map10' : {
      LEFT : (Transition(17, 31, 2, 'Map12', -17),
              Transition(37, 51, 2, 'Map11', -37),),
      RIGHT : (Transition(0, 14, 2, 'Map22', 0),
               Transition(35, 49, 2, 'Map8', -35),),
    },
    'Map11' : {
      LEFT : (Transition(0, 14, 2, 'Map13', 47),),
      RIGHT : (Transition(0, 14, 2, 'Map10', 37),),
    },
    'Map12' : {
      LEFT : (Transition(0, 14, 2, 'Map13', 27),),
      RIGHT : (Transition(0, 14, 2, 'Map10', 17),),
    },
    'Map13' : {
      LEFT : (Transition(64, 78, 2, 'Map14', -64),),
      RIGHT : (Transition(27, 41, 2, 'Map12', -27),
               Transition(47, 61, 2, 'Map11', -47),),
    },
    'Map14' : {
      RIGHT : (Transition(0, 14, 2, 'Map13', 64),),
    },
    'Map15' : {
      LEFT : (Transition(0, 7, 2, 'Map7', 64),
              Transition(65, 79, 2, 'Map21', -65),),
      RIGHT : (Transition(0, 7, 2, 'Map16', 42),
               Transition(45, 59, 2, 'Map24', -45),),
    },
    'Map16' : {
      LEFT : (Transition(42, 49, 2, 'Map15', -42),),
      RIGHT : (Transition(6, 20, 2, 'Map17', -6),
               Transition(35, 49, 2, 'Map23', -35),),
      UP : (Transition(0, 39, 2, 'Map9', 20),),
    },
    'Map17' : {
      LEFT : (Transition(0, 14, 2, 'Map16', 6),),
      RIGHT : (Transition(0, 14, 2, 'Map20', 27),),
      DOWN : (Transition(47, 66, 2, 'Map18', -47),),
    },
    'Map18' : {
      UP : (Transition(0, 19, 2, 'Map17', 47),),
    },
    'Map19' : {
      LEFT : (Transition(0, 14, 2, 'Map9', 6),),
      RIGHT : (Transition(2, 14, 2, 'Map20', -3),),
    },
    'Map20' : {
      LEFT : (Transition(0, 12, 2, 'Map19', 3),
              Transition(27, 41, 2, 'Map17', -27),),
    },
    'Map21' : {
      LEFT : (Transition(0, 14, 2, 'Map5', 45),),
      RIGHT : (Transition(0, 14, 2, 'Map15', 65),),
    },
    'Map22' : {
      LEFT : (Transition(0, 14, 2, 'Map10', 0),),
    },
    'Map23' : {
      LEFT : (Transition(0, 14, 2, 'Map16', 35),),
      DOWN : (Transition(20, 39, 2, 'Map25', -20),),
    },
    'Map24' : {
      LEFT : (Transition(0, 14, 2, 'Map15', 45),),
      RIGHT : (Transition(0, 14, 2, 'Map25', 21),),
    },
    'Map25' : {
      LEFT : (Transition(21, 35, 2, 'Map24', -21),),
      RIGHT : (Transition(105, 119, 2, 'Map26', -107),),
      UP : (Transition(0, 19, 2, 'Map23', 20),),
    },
    'Map26' : {
      LEFT : (Transition(0, 14, 2, 'Map25', 107),),
    },
  },
}