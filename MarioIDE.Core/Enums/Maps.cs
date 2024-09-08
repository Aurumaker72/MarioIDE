// ReSharper disable InconsistentNaming

namespace MarioIDE.Core.Enums;

public static class Maps
{
    private struct MapInfo
    {
        public readonly string Name;
        public readonly short Level;
        public readonly byte Area;

        public MapInfo(string name, short level, byte area)
        {
            Name = name;
            Level = level;
            Area = area;
        }
    }

    private static readonly List<MapInfo> maps = new()
    {
        new MapInfo("Castle Grounds", 16, 1),
        new MapInfo("Castle Grounds", 0, 0),
        new MapInfo("Castle", 6, 1),
        new MapInfo("Castle", 6, 1),
        new MapInfo("Castle", 6, 3),
        new MapInfo("Castle", 6, 2),
        new MapInfo("Castle Courtyard", 26, 1),
        new MapInfo("Bob-omb Battlefield", 9, 1),
        new MapInfo("Whomp's Fortress", 24, 1),
        new MapInfo("Whomp's Fortress", 24, 1),
        new MapInfo("Jolly Roger Bay", 12, 1),
        new MapInfo("Jolly Roger Bay", 12, 1),
        new MapInfo("Jolly Roger Bay", 12, 2),
        new MapInfo("Cool, Cool Mountain", 5, 1),
        new MapInfo("Cool, Cool Mountain", 5, 2),
        new MapInfo("Big Boo's Haunt", 4, 1),
        new MapInfo("Big Boo's Haunt", 4, 1),
        new MapInfo("Big Boo's Haunt", 4, 1),
        new MapInfo("Big Boo's Haunt", 4, 1),
        new MapInfo("Big Boo's Haunt", 4, 1),
        new MapInfo("Hazy Maze Cave", 7, 1),
        new MapInfo("Hazy Maze Cave", 7, 1),
        new MapInfo("Lethal Lava Land", 22, 1),
        new MapInfo("Lethal Lava Land", 22, 2),
        new MapInfo("Lethal Lava Land", 22, 2),
        new MapInfo("Shifting Sand Land", 8, 1),
        new MapInfo("Shifting Sand Land", 8, 2),
        new MapInfo("Shifting Sand Land", 8, 2),
        new MapInfo("Shifting Sand Land", 8, 2),
        new MapInfo("Shifting Sand Land", 8, 2),
        new MapInfo("Dire, Dire Docks", 23, 1),
        new MapInfo("Dire, Dire Docks", 23, 2),
        new MapInfo("Dire, Dire Docks", 23, 1),
        new MapInfo("Snowman's Land", 10, 1),
        new MapInfo("Snowman's Land", 10, 2),
        new MapInfo("Wet-Dry World", 11, 1),
        new MapInfo("Wet-Dry World", 11, 1),
        new MapInfo("Wet-Dry World", 11, 1),
        new MapInfo("Tall, Tall Mountain", 36, 1),
        new MapInfo("Tall, Tall Mountain", 36, 2),
        new MapInfo("Tall, Tall Mountain", 36, 2),
        new MapInfo("Tall, Tall Mountain", 36, 2),
        new MapInfo("Tiny-Huge Island", 13, 1),
        new MapInfo("Tiny-Huge Island", 13, 2),
        new MapInfo("Tiny-Huge Island", 13, 3),
        new MapInfo("Tiny-Huge Island", 13, 3),
        new MapInfo("Tiny-Huge Island", 13, 3),
        new MapInfo("Tick Tock Clock", 14, 1),
        new MapInfo("Tick Tock Clock", 14, 1),
        new MapInfo("Tick Tock Clock", 14, 1),
        new MapInfo("Tick Tock Clock", 14, 1),
        new MapInfo("Tick Tock Clock", 14, 1),
        new MapInfo("Rainbow Ride", 15, 1),
        new MapInfo("Tower of the Wing Cap", 29, 1),
        new MapInfo("Vanish Cap under the Moat", 18, 1),
        new MapInfo("Cavern of the Metal Cap", 28, 1),
        new MapInfo("The Princess's Secret Slide", 27, 1),
        new MapInfo("The Princess's Secret Slide", 27, 1),
        new MapInfo("The Secret Aquarium", 20, 1),
        new MapInfo("Wing Mario over the Rainbow", 31, 1),
        new MapInfo("Bowser in the Dark World", 17, 1),
        new MapInfo("Bowser in the Dark World", 30, 1),
        new MapInfo("Bowser in the Fire Sea", 19, 1),
        new MapInfo("Bowser in the Fire Sea", 19, 1),
        new MapInfo("Bowser in the Fire Sea", 19, 1),
        new MapInfo("Bowser in the Fire Sea", 19, 1),
        new MapInfo("Bowser in the Fire Sea", 33, 1),
        new MapInfo("Bowser in the Sky", 21, 1),
        new MapInfo("Bowser in the Sky", 21, 1),
        new MapInfo("Bowser in the Sky", 21, 1),
        new MapInfo("Bowser in the Sky", 21, 1),
        new MapInfo("Bowser in the Sky", 34, 1)
    };

    public static string GetBestMap(short level, byte area)
    {
        foreach (MapInfo map in maps)
        {
            if (map.Level == level && map.Area == area)
            {
                return map.Name;
            }
        }
        return "UNKNOWN-MAP";
    }
}