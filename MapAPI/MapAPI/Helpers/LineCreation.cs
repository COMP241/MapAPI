using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using MapAPI.Models;
using Newtonsoft.Json;

namespace MapAPI.Helpers
{
    public static class LineCreation
    {
        private static readonly ConfigFile.Config Config;

        static LineCreation()
        {
            Config = JsonConvert.DeserializeObject<ConfigFile.Config>(File.ReadAllText("config.json"));
        }

        /// <summary>
        ///     Finds all lines in this 2-dimensional Array of Booleans.
        /// </summary>
        /// <returns>A List of Lists of Points that form all the lines found.</returns>
        public static List<List<PointF>> CreateLineParts(this bool[][] points)
        {
            int height = points.Length;
            int width = points[0].Length;
            List<List<PointF>> partialLines = new List<List<PointF>>();
            HashSet<PointF> usedPoints = new HashSet<PointF>();

            //Goes through unneeded points
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

                    //Checks values are in bounds
                    if (x + xOffset < 0 || x + xOffset >= width || y + yOffset < 0 || y + yOffset >= height) continue;

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
                            //Checks another branch didn't use the possibility
                            //if (usedPoints.Contains(possibility)) continue; //Removed cause it might fix a problem
                            CalculateLines(currentPoint, possibility);
                        break;
                    }
                }

                //Adds the line to the list of lines
                partialLines.Add(baseLine);
                //Returns the line for some cases where it is needed
                return baseLine;
            }
        }

        /// <summary>
        ///     Takes out a large number of the Points from this List of Lists of Points while maintaining the general shape.
        /// </summary>
        public static void ReduceLines(this List<List<PointF>> lines)
        {
            int initialProportion = Config.LineReduction.InitialCut;
            double angelLimit = Config.LineReduction.AngleLimit;

            foreach (List<PointF> line in lines)
            {
                if (line.Count < 3) continue;

                InitialCut(line);
                AngleCut(line);
            }

            //Leaves only every initialProportion-th point
            void InitialCut(List<PointF> line)
            {
                int startCount = line.Count;
                //Counts down from second to last point
                for (int i = line.Count - 2; i >= 0; i--)
                {
                    //Leaves point if it is a initialProportion-th point so long as it isn't within the last half initialProportion points
                    if (i % initialProportion == 0 && (i < startCount - initialProportion / 2 || i == 0)) continue;

                    //Otherwise removes point
                    line.RemoveAt(i);
                }
            }

            //Removes points that form an angle less than angleLimit
            void AngleCut(List<PointF> line)
            {
                int index = 0;
                while (index <= line.Count - 3)
                {
                    //Hypotenuse from Pythagoras
                    double a = Math.Sqrt((line[index].X - line[index + 2].X) *
                                         (line[index].X - line[index + 2].X) +
                                         (line[index].Y - line[index + 2].Y) *
                                         (line[index].Y - line[index + 2].Y));
                    //Side 1 from Pythagoras
                    double b = Math.Sqrt((line[index].X - line[index + 1].X) *
                                         (line[index].X - line[index + 1].X) +
                                         (line[index].Y - line[index + 1].Y) *
                                         (line[index].Y - line[index + 1].Y));
                    //Side 2 from Pythagoras
                    double c = Math.Sqrt((line[index + 1].X - line[index + 2].X) *
                                         (line[index + 1].X - line[index + 2].X) +
                                         (line[index + 1].Y - line[index + 2].Y) *
                                         (line[index + 1].Y - line[index + 2].Y));

                    //Angle from cos law
                    double angle = Math.Acos((b * b + c * c - a * a) / (2 * b * c));

                    //Delete if greater than angle limit
                    if (angle > angelLimit)
                        line.RemoveAt(index + 1);
                    //Otherwise move on
                    else
                        index++;
                }
            }
        }

        /// <summary>
        ///     Goes through this List of Lists of Points creating loops and removing ant List of Points that form a loop from this
        ///     List.
        /// </summary>
        /// <returns>A List of Lists of Points that contains the loops found.</returns>
        public static List<List<PointF>> CreateLoops(this List<List<PointF>> lines)
        {
            List<List<PointF>> loops = new List<List<PointF>>();
            for (int i = 0; i < lines.Count; i++)
            {
                List<PointF> line = lines[i];

                //Skips lines that only have 1 point
                if (line.Count == 1) continue;


                List<List<PointF>> usedLines = new List<List<PointF>> {line};
                List<List<PointF>> linesInLoop = new List<List<PointF>>();
                //Tries to create a loop
                List<PointF> loop = CreateLoop(line, line.Last(), line[0], usedLines, ref linesInLoop);
                //Continues if no loop was found
                if (loop == null) continue;

                //If any of the points are within 5 units of all other points it doesn't make the loop as it is probably just a mistake unless the loop connects to nothing else
                int minLoopSize = Config.MinLoopSize;
                if (loop.Any(point => loop.Aggregate(true,
                        (current, x) => current && Math.Abs(point.X - x.X) < minLoopSize &&
                                        Math.Abs(point.Y - x.Y) < minLoopSize)) &&
                    linesInLoop.Any(x => lines.Count(y => MatchingEnds(x, y)) >= 3))
                {
                    BreakLoop(linesInLoop);
                    continue;
                }

                //Removes all but the last line
                linesInLoop.GetRange(0, linesInLoop.Count - 1).ForEach(x => lines.Remove(x));
                //Updates the value of i to accommodate for the removed items
                i = lines.IndexOf(linesInLoop.Last()) - 1;
                //Removes the last item
                lines.Remove(linesInLoop.Last());

                loop.RemoveAt(0);
                loops.Add(loop);
            }

            loops.Sort((loop1, loop2) => LengthOfLine(loop2).CompareTo(LengthOfLine(loop1)));

            return loops;

            //Recursively goes through looking for possible
            List<PointF> CreateLoop(List<PointF> currentSegment, PointF joiningPoint, PointF loopGoal,
                List<List<PointF>> usedLines, ref List<List<PointF>> linesInLoop)
            {
                //If the joining point is the end goal a loop has been found
                if (joiningPoint == loopGoal)
                {
                    linesInLoop.Add(currentSegment);
                    return currentSegment;
                }

                //Finds all lines that can join at the joining point
                List<List<PointF>> nextSegmentsStart =
                    lines.FindAll(line => line[0] == joiningPoint && !usedLines.Contains(line));
                List<List<PointF>> nextSegmentsEnd =
                    lines.FindAll(line => line.Last() == joiningPoint && !usedLines.Contains(line));
                usedLines.AddRange(nextSegmentsStart);
                usedLines.AddRange(nextSegmentsEnd);

                List<PointF> result = null;

                //Goes through trying to create a loop with each possible next segment
                for (int i = 0; i < nextSegmentsStart.Count && result == null; i++)
                {
                    List<PointF> nextSegment = nextSegmentsStart[i];
                    if (nextSegment.Count == 1) continue;
                    result = CreateLoop(nextSegment, nextSegment.Last(), loopGoal, usedLines, ref linesInLoop);
                }
                for (int i = 0; i < nextSegmentsEnd.Count && result == null; i++)
                {
                    List<PointF> nextSegment = nextSegmentsEnd[i];
                    if (nextSegment.Count == 1) continue;
                    result = CreateLoop(nextSegment, nextSegment[0], loopGoal, usedLines, ref linesInLoop);
                }

                //If a loop has been found
                if (result != null)
                {
                    linesInLoop.Add(currentSegment);
                    //Joins them together in whatever way they join together
                    currentSegment = currentSegment.JoinWith(result);

                    //Continues creating loop
                    return currentSegment;
                }

                //Nothing was founds
                return null;
            }

            //Breaks the loop be removing the longest segment that can be removed without disconnecting anything else
            void BreakLoop(List<List<PointF>> linesInLoop)
            {
                int startOfLongestSegmentIndex = -1;
                int endOfLongestSegmentIndex = -1;
                double lengthOfLongestSegment = 0;

                //Gets index of a line that has a connection outside the loop
                int startIndex = linesInLoop.IndexOf(linesInLoop.First(x => lines.Count(y => MatchingEnds(x, y)) >= 3));

                double[] lengths = linesInLoop.Select(LengthOfLine).ToArray();

                int? startOfSegmentIndex = null;
                int? endOfSegmentIndex = null;
                double lengthOfSegment = 0;
                int index = startIndex;
                do
                {
                    //Adds to lengths
                    lengthOfSegment += lengths[index];

                    //If this is the start of a new segment
                    if (startOfSegmentIndex == null)
                    {
                        //Assigns start
                        startOfSegmentIndex = index;
                        //Checks if it is also the end
                        if (lines.Count(x => x[0] == linesInLoop[index][0] || x.Last() == linesInLoop[index][0]) >= 3 &&
                            lines.Count(x => x[0] == linesInLoop[index].Last() ||
                                             x.Last() == linesInLoop[index].Last()) >= 3)
                            endOfSegmentIndex = startOfSegmentIndex;
                    }
                    else if (lines.Count(x => MatchingEnds(x, linesInLoop[index])) >= 3)
                    {
                        endOfSegmentIndex = index;
                    }

                    if (endOfSegmentIndex != null)
                    {
                        //If this is the new longest update values
                        if (lengthOfSegment > lengthOfLongestSegment)
                        {
                            lengthOfLongestSegment = lengthOfSegment;
                            startOfLongestSegmentIndex = (int) startOfSegmentIndex;
                            endOfLongestSegmentIndex = (int) endOfSegmentIndex;
                        }

                        //Resets values
                        startOfSegmentIndex = null;
                        endOfSegmentIndex = null;
                        lengthOfSegment = 0;
                    }

                    //Increments index
                    index++;
                    if (index == linesInLoop.Count)
                        index = 0;
                } while (index != startIndex);

                index = startOfLongestSegmentIndex;
                while (true)
                {
                    //Removes lines
                    lines.Remove(linesInLoop[index]);

                    //Breaks if end has been reached
                    if (index == endOfLongestSegmentIndex) break;

                    //Increments index
                    index++;
                    if (index == linesInLoop.Count)
                        index = 0;
                }
            }
        }

        /// <summary>
        ///     Connects then List of Points in this List together by their matching ends.
        /// </summary>
        public static void ConnectLines(this List<List<PointF>> lines)
        {
            lines.Sort((line1, line2) => LengthOfLine(line2).CompareTo(LengthOfLine(line1)));
            List<double> lengths = lines.Select(LengthOfLine).ToList();

            //Goes through each line from longest to shortest
            for (int i = 0; i < lines.Count; i++)
            {
                lengths[i] = FindLongest(lines[i], out List<List<PointF>> partsOfLongestLine);
                JoinLines(partsOfLongestLine);
                lines[0].Reverse();
                lengths[i] = FindLongest(lines[i], out partsOfLongestLine);
                JoinLines(partsOfLongestLine);
            }

            //Deletes short lines
            for (int i = 0; i < lines.Count; i++)
            {
                if (!(lengths[i] < Config.MinLineLength)) continue;

                lengths.RemoveAt(i);
                lines.RemoveAt(i);
                i--;
            }

            //Finds lines that make up the longest line
            double FindLongest(List<PointF> line, out List<List<PointF>> partsOfLongestLine)
            {
                PointF joinPoint = line.Last();
                //Possible next points
                List<List<PointF>> options = lines.Where(x => (joinPoint == x[0] || joinPoint == x.Last()) && x != line)
                    .ToList();

                //End of recursion, this line is at the end
                if (options.Count == 0)
                {
                    partsOfLongestLine = new List<List<PointF>> {line};
                    return lengths[lines.IndexOf(line)];
                }

                List<List<PointF>>[] fullOptions = new List<List<PointF>>[options.Count];
                double[] optionsLengths = new double[options.Count];
                //Gets longest line of each option
                for (int i = 0; i < options.Count; i++)
                {
                    List<PointF> option = options[i];
                    //Flips option if it is the end that matches with the joining point
                    if (option[0] != joinPoint)
                        option.Reverse();

                    optionsLengths[i] = FindLongest(option, out fullOptions[i]);
                }

                //Finds out which index had the longest line
                int longestOptionIndex = Array.IndexOf(optionsLengths, optionsLengths.Max());

                //Adds this line to the best option
                partsOfLongestLine = fullOptions[longestOptionIndex];
                partsOfLongestLine.Insert(0, line);

                return optionsLengths[longestOptionIndex] + lengths[lines.IndexOf(line)];
            }

            //Joins list of lines together
            void JoinLines(List<List<PointF>> linesToJoin)
            {
                //While there are lines to join
                while (linesToJoin.Count > 1)
                {
                    //Deletes next line from lines and deletes it's length
                    int lineToRemoveIndex = lines.IndexOf(linesToJoin[1]);
                    lines.RemoveAt(lineToRemoveIndex);
                    lengths.RemoveAt(lineToRemoveIndex);

                    //Joins to next line
                    linesToJoin[1].RemoveAt(0);
                    linesToJoin[0].AddRange(linesToJoin[1]);

                    //Removes joined line
                    linesToJoin.RemoveAt(1);
                }
            }
        }

        /// <summary>
        ///     Takes a series of lines and loops defined as a List of Points and turns it into a List of Lines based on the colors
        ///     from a bitmap.
        /// </summary>
        /// <param name="lines">List of Lists of Points that defines all the lines.</param>
        /// <param name="loops">List of Lists of Points that defines all the loops.</param>
        /// <param name="bitmap">Bitmap to source the colors off of.</param>
        /// <exception cref="ArgumentOutOfRangeException">At least one point falls out of the range of the bitmap.</exception>
        /// <returns>A List of Lines based on the points provided in lines and loops.</returns>
        public static List<Line> CreateLineObjects(List<List<PointF>> lines, List<List<PointF>> loops, Bitmap bitmap)
        {
            List<Line> output = new List<Line>();
            //Creates Line objects
            output.AddRange(lines.Select(line => new Line
            {
                Color = GetColor(line),
                Loop = false,
                Points = line.Select(point => new PointF(point.X / bitmap.Width, point.Y / bitmap.Height)).ToList()
            }));
            output.AddRange(loops.Select(loop => new Line
            {
                Color = GetColor(loop),
                Loop = true,
                Points = loop.Select(point => new PointF(point.X / bitmap.Width, point.Y / bitmap.Height)).ToList()
            }));

            return output;

            //Gets the color as an int defined by MapAPI.Models.Line.Colors from the median hue of a set of points using bitmap as the reference
            int GetColor(List<PointF> points)
            {
                List<Color> colorSamples;
                try
                {
                    colorSamples = points.Select(point => bitmap.GetPixel((int) point.X, (int) point.Y)).ToList();
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new ArgumentOutOfRangeException(nameof(bitmap),
                        "At least one point falls out of the range of the bitmap.");
                }

                //Checks for black using median brightness and saturation
                List<float> brightnessSamples = colorSamples.Select(color => color.GetBrightness()).ToList();
                List<float> saturationSamples = colorSamples.Select(color => color.GetSaturation()).ToList();
                brightnessSamples.Sort();
                saturationSamples.Sort();
                float medianBrightness = brightnessSamples[brightnessSamples.Count / 2];
                float medianSaturation = saturationSamples[saturationSamples.Count / 2];
                if (medianSaturation < 0.3 || medianBrightness < 0.3)
                    return Line.Colors.Black;

                //Gets median hue and maps it to a color
                List<float> hueSamples = colorSamples.Select(color => color.GetHue()).ToList();
                hueSamples.Sort();
                float medianHue = hueSamples[hueSamples.Count / 2];

                int hueRegion = (int) (medianHue / 30);

                //Picks the hue based on the region
                switch (hueRegion)
                {
                    case 0:
                        return Line.Colors.Red;
                    case 1:
                        return Line.Colors.Yellow;
                    case 2:
                        return Line.Colors.Yellow;
                    case 3:
                        return Line.Colors.Green;
                    case 4:
                        return Line.Colors.Green;
                    case 5:
                        return Line.Colors.Cyan;
                    case 6:
                        return Line.Colors.Cyan;
                    case 7:
                        return Line.Colors.Blue;
                    case 8:
                        return Line.Colors.Blue;
                    case 9:
                        return Line.Colors.Magenta;
                    case 10:
                        return Line.Colors.Magenta;
                    case 11:
                        return Line.Colors.Red;
                    default:
                        return Line.Colors.Red;
                }
            }
        }

        /// <summary>
        ///     Joins together this Lists of Points with another one by their matching ends.
        /// </summary>
        /// <param name="line">The List of Points to be joined onto the end of this List of Points.</param>
        /// <exception cref="ArgumentException">The two Lists didn't have a set of matching ends.</exception>
        /// <returns>A new List of Points starting with the elements in this List of Points followed by the elements in line.</returns>
        private static List<PointF> JoinWith(this List<PointF> mainLine, List<PointF> line)
        {
            List<PointF> line1 = mainLine.ToList();
            List<PointF> line2 = line.ToList();

            if (line1.Count == 0)
                return line2;
            if (line2.Count == 0)
                return line1;

            //Looks for which two points match
            if (line1[0] == line2[0])
            {
                line1.Reverse();
                line2.RemoveAt(0);
                line1.AddRange(line2);
            }
            else if (line1[0] == line2.Last())
            {
                line1.Reverse();
                line2.Reverse();
                line2.RemoveAt(0);
                line1.AddRange(line2);
            }
            else if (line1.Last() == line2[0])
            {
                line2.RemoveAt(0);
                line1.AddRange(line2);
            }
            else if (line1.Last() == line2.Last())
            {
                line2.Reverse();
                line2.RemoveAt(0);
                line1.AddRange(line2);
            }

            if (mainLine.Count == line1.Count)
                throw new ArgumentException("The two Lists didn't have a set of matching ends.", nameof(line));
            return line1;
        }

        /// <summary>
        ///     Calculates the total distance between all of the points in a List of Points.
        /// </summary>
        /// <param name="line">The List of Points to calculate the distance with.</param>
        /// <returns>The sum of the distance between the points.</returns>
        private static double LengthOfLine(List<PointF> line)
        {
            double length = 0;
            for (int i = 0; i < line.Count - 1; i++)
                length += Math.Sqrt((line[i].X - line[i + 1].X) * (line[i].X - line[i + 1].X) +
                                    (line[i].Y - line[i + 1].Y) * (line[i].Y - line[i + 1].Y));

            return length;
        }

        /// <summary>
        ///     Checks if two Lists of Points have at least one set of similar ends.
        /// </summary>
        /// <param name="line1">One line to use in the comparison.</param>
        /// <param name="line2">One line to use in the comparison.</param>
        /// <returns>True if a set of similar ends was found.</returns>
        private static bool MatchingEnds(List<PointF> line1, List<PointF> line2)
        {
            return line1[0] == line2[0] || line1[0] == line2.Last() || line1.Last() == line2[0] ||
                   line1.Last() == line2.Last();
        }
    }
}