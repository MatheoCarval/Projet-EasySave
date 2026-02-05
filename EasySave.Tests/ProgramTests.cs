using Xunit;

namespace EasySave.Tests
{
    /// <summary>
    /// Unit tests for the Program class entry point, verifying class existence and main method structure.
    /// </summary>
    public class ProgramTests
    {
        /// <summary>
        /// Verifies that the Program class exists and is properly defined.
        /// </summary>
        [Fact]
        public void Program_ClassExists()
        {
            var programType = typeof(EasySave.Program);

            Assert.NotNull(programType);
        }

        /// <summary>
        /// Verifies that the Program class contains a static Main method for application entry point.
        /// </summary>
        [Fact]
        public void Program_HasMainMethod()
        {
            var programType = typeof(EasySave.Program);

            var mainMethod = programType.GetMethod("Main", 
                System.Reflection.BindingFlags.Static | 
                System.Reflection.BindingFlags.NonPublic);

            Assert.NotNull(mainMethod);
        }
    }
}