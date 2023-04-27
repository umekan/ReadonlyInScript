using System.Xml.Serialization;
using umekan;

namespace TestSpace
{
    public static class TestClass
    {
        public static void Main(string[] args) { }
    }

    public class Program
    {
        [ReadonlyInScript] private int _field;
        [field: ReadonlyInScript] private int _prop { get; set; }
        
        private void TestMethod()
        {
            var temp = _field; // 許される
            _field = 1; // 許されない
            _field++; // 許されない
            _field += 1; // 許されない

            temp = _prop; // 許される
            _prop = 1; // 許されない
            _prop++; // 許されない
            _prop += 1; // 許されない
            var f = () => _prop++; // 許されない
        }
    }
}