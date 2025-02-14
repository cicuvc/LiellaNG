using Liella.TypeAnalysis.Metadata;

namespace Liella.Driver {
    internal class App {
        static void Main(string[] args) {
            var stream = new FileStream("./Payload.dll", FileMode.Open);
            var icu = new ImageImporter(stream, (asmName) => {
                return new FileStream($"./{asmName.Name}.dll", FileMode.Open);
            });

            icu.BuildTypeEnvironment();


            Console.ReadLine();
        }
    }
}
