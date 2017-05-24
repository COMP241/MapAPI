using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MapAPI.Models;

namespace MapAPI.Helpers
{
    public static class LineCreation
    {
        public static List<List<PointF>> CreateLines(this bool[][] points)
        {
            int height = points.Length;
            int width = points[0].Length;
            List<Line> lines = new List<Line>();
            List<List<PointF>> partialLines = new List<List<PointF>>();
            HashSet<PointF> usedPoints = new HashSet<PointF>();
            //Goes through finding zigzag patters
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (points[y][x])
                {
                    List<PointF> surrounding = GetAdjacent(x, y);

                    //If there are 3 or more surrounding pixels check if any can be removed
                    if (surrounding.Count < 3) continue;
                    if (RemoveUnneeded(x, y))
                        //And then x needs to be decreased so that that point will be tested again
                        x--;
                }

            //Goes through creating lines
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                PointF basePoint = new PointF(x, y);
                if (points[y][x] && !usedPoints.Contains(basePoint))
                {
                    List<PointF> adjacentPoints = GetAdjacent(x, y);
                    switch (adjacentPoints.Count)
                    {
                        case 0:
                            //Ignores single points
                            continue;
                        case 1:
                            //Start of a line was found so creates a lines from there
                            CalculateLines(basePoint, adjacentPoints[0]);
                            break;
                        case 2:
                            //Middle of a line was found so creates line in both directions and joins them
                            List<PointF> line1 = CalculateLines(basePoint, adjacentPoints[0]);
                            List<PointF> line2 = CalculateLines(basePoint, adjacentPoints[1]);
                            line1.Reverse();
                            line2.RemoveAt(0);
                            line1.AddRange(line2);
                            partialLines.Remove(line2);
                            break;
                        default:
                            //A junction was found so creates a line in each direction
                            foreach (PointF adjacentPoint in adjacentPoints)
                                CalculateLines(basePoint, adjacentPoint);
                            break;
                    }
                }
            }

            return partialLines;

            //Gets a list of the points surrounding point x,y
            List<PointF> GetAdjacent(int x, int y)
            {
                List<PointF> output = new List<PointF>();
                for (int xOffset = -1; xOffset <= 1; xOffset++)
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    int xSum = x + xOffset;
                    int ySum = y + yOffset;

                    //Checks for inbound and not 0,0 offset
                    if (xOffset == 0 && yOffset == 0 || xSum < 0 || xSum >= width || ySum < 0 ||
                        ySum >= height) continue;

                    if (points[ySum][xSum])
                        output.Add(new PointF(xSum, ySum));
                }

                return output;
            }

            //Checks for any unneeded pixels around x,y
            bool RemoveUnneeded(int x, int y)
            {
                for (int offset = -1; offset <= 5; offset += 2)
                {
                    int xOffset;
                    int yOffset;
                    if (offset <= 1)
                    {
                        xOffset = offset;
                        yOffset = 0;
                    }
                    else
                    {
                        xOffset = 0;
                        yOffset = offset - 4;
                    }
                    //Checks if there is a point there
                    if (!points[y + yOffset][x + xOffset]) continue;

                    List<PointF> adjacentPoints = GetAdjacent(x + xOffset, y + yOffset);

                    //Skips if there aren't multiple points
                    if (adjacentPoints.Count < 2) continue;

                    bool success = true;
                    List<PointF> connectedPoints = new List<PointF> {adjacentPoints[0]};
                    adjacentPoints.RemoveAt(0);
                    while (success)
                    {
                        success = false;

                        //Foreach adjacent point
                        for (int i = 0; i < adjacentPoints.Count; i++)
                        {
                            PointF adjacentPoint = adjacentPoints[i];
                            //Check against each currently found to be connected point
                            for (int j = 0; j < connectedPoints.Count; j++)
                            {
                                PointF connectedPoint = connectedPoints[j];
                                //And see if it is connected to it
                                if (Math.Abs(connectedPoint.X - adjacentPoint.X) <= 1 &&
                                    Math.Abs(connectedPoint.Y - adjacentPoint.Y) <= 1)
                                {
                                    //If it is add it to connectedPoints and remove from adjacentPoints
                                    connectedPoints.Add(adjacentPoint);
                                    adjacentPoints.Remove(adjacentPoint);
                                    success = true;
                                    //Subtract 1 from i as the current index has been removed so it needs to do that index again
                                    i--;
                                    break;
                                }
                            }
                        }
                    }

                    //If nothing is in adjacentPoints then all the adjacentPoints were found to be connected so the center point isn't needed
                    if (adjacentPoints.Count == 0)
                    {
                        points[y + yOffset][x + xOffset] = false;
                        return true;
                    }
                }

                return false;
            }

            //Creates a line from a point with a set next point and branches off when junctions are hit
            List<PointF> CalculateLines(PointF basePoint, PointF direction)
            {
                List<PointF> baseLine = new List<PointF> {basePoint, direction};
                usedPoints.Add(basePoint);
                usedPoints.Add(direction);

                PointF currentPoint = direction;
                while (true)
                {
                    //Gets all surrounding points that haven't been used
                    List<PointF> possibilities = GetAdjacent((int) currentPoint.X, (int) currentPoint.Y)
                        .Where(point => !usedPoints.Contains(point)).ToList();

                    if (possibilities.Count == 0)
                        break;
                    if (possibilities.Count == 1)
                    {
                        //Sets the single possibility to be next point
                        currentPoint = possibilities[0];
                        usedPoints.Add(currentPoint);
                        baseLine.Add(currentPoint);
                    }
                    else
                    {
                        //Creates a branch from all possibilities
                        foreach (PointF possibility in possibilities)
                        {
                            //Checks another branch didn't use the possibility
                            if (usedPoints.Contains(possibility)) continue;
                            CalculateLines(currentPoint, possibility);
                        }
                        break;
                    }
                }

                //Adds the line to the list of lines
                partialLines.Add(baseLine);
                //Returns the line for some cases where it is needed
                return baseLine;
            }
        }
    }
}