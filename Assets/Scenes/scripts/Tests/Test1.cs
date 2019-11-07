using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEngine
{
    public class Test1
    {
        [Test]
        public void test1()
        {
            List<int> skillList = new List<int>();
            Debug.Log(int.Parse(string.Join("", skillList)));
        }
    }
}