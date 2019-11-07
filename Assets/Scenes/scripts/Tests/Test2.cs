using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class Test2
    {
        // A Test behaves as an ordinary method
        [Test]
        public void Test2SimplePasses()
        {
            // Use the Assert class to test conditions
            List<int> skillList = new List<int>();
            Debug.Log(int.Parse(string.Join("", skillList)));
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator Test2WithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
