using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Liella.TypeAnalysis.Utils {
    public static class SequenceHelpers {
        public static IEnumerable<T> InclusiveCumulativeSum<T>(this IEnumerable<T> sequence) where T : struct, IAdditiveIdentity<T,T>,IAdditionOperators<T, T, T> {
            var sum = T.AdditiveIdentity;
            return sequence.Select(e => {
                sum += e;
                return sum;
            });
        }
        public static IEnumerable<T> ExclusiveCumulativeSum<T>(this IEnumerable<T> sequence) where T : struct, IAdditiveIdentity<T, T>, IAdditionOperators<T, T, T> {
            var sum = T.AdditiveIdentity;
            return sequence.Select(e => {
                var result = sum;
                sum += e;
                return result;
            });
        }
    }
}
