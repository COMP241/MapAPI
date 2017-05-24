using System;
using System.Collections.Generic;
using System.Linq;

namespace MapAPI.Helpers
{
    /// <summary>
    ///     Based on the code by TaW, http://stackoverflow.com/a/26120310
    ///     under the CC BY-SA license, https://creativecommons.org/licenses/by-sa/4.0/.
    /// </summary>
    public static class Thinning
    {
        public static void ZhangSuenThinning(this bool[][] s)
        {
            bool[][] temp = ArrayClone(s);
            int count;
            do
            {
                count = Step(1, temp, s);
                temp = ArrayClone(s);
                count += Step(2, temp, s);
                temp = ArrayClone(s);
            } while (count > 0);
        }

        private static int Step(int stepNo, bool[][] temp, bool[][] s)
        {
            int count = 0;

            for (int a = 1; a < temp.Length - 1; a++)
            for (int b = 1; b < temp[0].Length - 1; b++)
                if (SuenThinningAlg(a, b, temp, stepNo == 2))
                {
                    if (s[a][b]) count++;
                    s[a][b] = false;
                }
            return count;
        }

        private static bool SuenThinningAlg(int x, int y, bool[][] s, bool even)
        {
            bool p2 = s[x][y - 1];
            bool p4 = s[x + 1][y];
            bool p6 = s[x][y + 1];
            bool p8 = s[x - 1][y];


            int bp1 = NumberOfNonZeroNeighbors(x, y, s);
            if (bp1 < 2 || bp1 > 6) return false;
            if (NumberOfZeroToOneTransitionFromP9(x, y, s) != 1) return false;
            if (even)
            {
                if (p2 && p4 && p8) return false;
                if (!(p2 && p6 && p8))
                    return true;
            }
            else
            {
                if (p2 && p4 && p6) return false;
                if (!(p4 && p6 && p8))
                    return true;
            }
            return false;
        }

        private static int NumberOfZeroToOneTransitionFromP9(int x, int y, bool[][] s)
        {
            bool p2 = s[x][y - 1];
            bool p3 = s[x + 1][y - 1];
            bool p4 = s[x + 1][y];
            bool p5 = s[x + 1][y + 1];
            bool p6 = s[x][y + 1];
            bool p7 = s[x - 1][y + 1];
            bool p8 = s[x - 1][y];
            bool p9 = s[x - 1][y - 1];

            int a = Convert.ToInt32(!p2 && p3) + Convert.ToInt32(!p3 && p4) +
                    Convert.ToInt32(!p4 && p5) + Convert.ToInt32(!p5 && p6) +
                    Convert.ToInt32(!p6 && p7) + Convert.ToInt32(!p7 && p8) +
                    Convert.ToInt32(!p8 && p9) + Convert.ToInt32(!p9 && p2);
            return a;
        }

        private static int NumberOfNonZeroNeighbors(int x, int y, IReadOnlyList<bool[]> s)
        {
            int count = 0;
            if (s[x - 1][y]) count++;
            if (s[x - 1][y + 1]) count++;
            if (s[x - 1][y - 1]) count++;
            if (s[x][y + 1]) count++;
            if (s[x][y - 1]) count++;
            if (s[x + 1][y]) count++;
            if (s[x + 1][y + 1]) count++;
            if (s[x + 1][y - 1]) count++;
            return count;
        }

        public static T[][] ArrayClone<T>(T[][] array)
        {
            return array.Select(a => a.ToArray()).ToArray();
        }
    }
}