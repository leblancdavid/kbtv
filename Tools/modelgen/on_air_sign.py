"""Wall-mount sign kept hidden in the current World3D presentation."""
from common import box, label, station_palette


def build():
    p = station_palette()
    box('Metal sign housing', (0, 0, 0.15), (0.9, 0.08, 0.3), p['shell'], 0.014)
    box('Dark red lens', (0, 0.042, 0.15), (0.82, 0.012, 0.23), p['black'])
    label('On air lettering', 'ON AIR', (0, 0.051, 0.15), 0.17, p['red'])
