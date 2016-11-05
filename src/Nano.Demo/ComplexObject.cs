using System;
using System.Collections.Generic;
#pragma warning disable 1591

namespace Nano.Demo
{
    /// <summary>
    /// Customer API.
    /// </summary>
    public class ComplexObject
    {
        public static ComplexModel UpdateComplexModel(ComplexModel complexModel)
        {
            if (complexModel == null)
                throw new ArgumentNullException("complexModel");

            return complexModel;
        }

        /// <summary>
        /// Parent Model
        /// </summary>
        public class ComplexModel
        {

            //check for enums
            public string stringType;
            public DateTime dateType;
            public DateTime? dateOptional;
            public int intType;
            public Int32 int32Type;
            public Int16 int16Type;
            public Int64 int64Type;
            public int? intOptional;
            public Int32? int32Optional;
            public Int16? int16Optional;
            public decimal decimalType;
            public decimal? decimalOptional;
            public long longType;
            public long? longOptional;
            public char charType;
            public char[] charArray;
            public bool boolType;
            public Boolean booleanType;
            public Boolean[] booleanArray;
            public bool[] boolArray;
            public float floatType;
            public float? floatOptional;
            public List<Int32> int32List;
            public List<ChildModel> ListOfChild;
            public IList<ChildModel> IListOfChild;
            public ChildModel[] ChildArray;
            public SiblingModel SiblingType;
            public Dictionary<string, string> DictionaryOfStringString { get; set; }
            List<List<int>> ListOfListOfInt { get; set; }
            List<Dictionary<int, object>> listOfDictionaryParm { get; set; }
            Dictionary<int, Dictionary<int, object>> dictionaryOfDictionaryParm { get; set; }
            IEnumerable<int> intIEnumerable { get; set; }
            Tuple<int, string, object> tuple { get; set; }
            Func<int, int, int, int, int> funcParam { get; set; }
            Func<Func<Func<Func<int>>>> veryNestedfuncParam { get; set; }
        }

        public class ChildModel
        {
            public string stringType;
            public string stringType2;
            public SiblingModel siblingModelType;
        }

        public class SiblingModel
        {
            public string stringType;
            public string stringType2;
            public ChildModel[] childModelType;
            public ComplexModel complexModelType;
        }
    }
}