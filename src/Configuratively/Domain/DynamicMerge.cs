using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dynamitey;

namespace Configuratively.Domain
{
    internal static class DynamicMerge
    {
        public static dynamic DoMerge(dynamic left, dynamic right)
        {
            return Merge(left, right);
        }

        #region Overloads to handle merging of mismatched collection types

        private static dynamic Merge(IEnumerable<ExpandoObject> left, IEnumerable<ExpandoObject> right)
        {
            return Merge(left.ToArray(), right.ToArray());
        }

        private static dynamic Merge(List<Object> left, List<Object> right)
        {
            if (left.OfType<String>().Any())
            {
                return Merge(left.Cast<String>(), right.Cast<String>());
            }

            return Merge(left.Cast<ExpandoObject>(), right.Cast<ExpandoObject>());
        }

        #endregion

        # region Overloads to handle merging of primitive types
        private static dynamic Merge(string left, string right)
        {
            // For primitive types with the same key, the left side takes precedence
            return left;
        }

        private static dynamic Merge(int left, int right)
        {
            // For primitive types with the same key, the left side takes precedence
            return left;
        }

        private static dynamic Merge(long left, long right)
        {
            // For primitive types with the same key, the left side takes precedence
            return left;
        }

        private static dynamic Merge(float left, float right)
        {
            // For primitive types with the same key, the left side takes precedence
            return left;
        }
        private static dynamic Merge(bool left, bool right)
        {
            // For primitive types with the same key, the left side takes precedence
            return left;
        }
        private static dynamic Merge(IEnumerable<string> left, IEnumerable<string> right)
        {
            // Merge simple string arrays, removing any duplicates
            return left.Concat(right).Distinct().ToArray();
        }
        #endregion

        private static dynamic Merge(ExpandoObject[] col1, ExpandoObject[] col2)
        {
            var result = new List<ExpandoObject>();

            // Find the common collections
            var col1Group = col1.GroupBy(c => Dynamic.InvokeGet(c, "name") as string).ToList();
            var col2Group = col2.GroupBy(c => Dynamic.InvokeGet(c, "name") as string).ToList();
            var intersection = col1Group.Join(col2Group, cg1 => cg1.Key, cg2 => cg2.Key, (cg1, cg2) => new { left = cg1, right = cg2 }).ToList();

            var col1Keys = col1Group.Select(c2 => c2.Key);
            var col2Keys = col2Group.Select(c2 => c2.Key);

            // Take the left-only collections
            var col1Only = col1Group.Where(c => !col2Keys.Contains(c.Key)).ToList();
            col1Only.ToList().ForEach(c => c.ToList().ForEach(result.Add));

            // Merge the intersection
            foreach (var i in intersection)
            {
                result.Add(Merge(i.left.First(), i.right.First()));
            }

            // Take the right-only collections
            var col2Only = col2Group.Where(c => !col1Keys.Contains(c.Key));
            col2Only.ToList().ForEach(c => c.ToList().ForEach(result.Add));

            return result.ToArray();
        }

        private static dynamic Merge(ExpandoObject left, ExpandoObject right)
        {
            IDictionary<string, object> result = new ExpandoObject();

            // Find the common collections
            var leftMembers = Dynamic.GetMemberNames(left).ToList();
            var rightMembers = Dynamic.GetMemberNames(right).ToList();
            var intersection = leftMembers.Join(rightMembers, l => l, r => r, (l, r) => new { left = l, right = r }).ToList();

            // Take the left-only collections
            var leftOnly = leftMembers.Where(name => !rightMembers.Contains(name)).ToList();
            leftOnly.ForEach(l => result.Add(l, Dynamic.InvokeGet(left, l)));

            // Merge the intersection
            foreach (var i in intersection)
            {
                var leftToMerge = Dynamic.InvokeGet(left, i.left);
                var rightToMerge = Dynamic.InvokeGet(right, i.right);

                result.Add(i.left, Merge(leftToMerge, rightToMerge));
            }

            // Take the right-only collections
            var rightOnly = rightMembers.Where(name => !leftMembers.Contains(name)).ToList();
            rightOnly.ForEach(r => result.Add(r, Dynamic.InvokeGet(right, r)));


            return result as ExpandoObject;
        }
    }
}
