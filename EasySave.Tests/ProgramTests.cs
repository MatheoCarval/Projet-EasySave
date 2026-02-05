using Xunit;

namespace EasySave.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void Program_ClassExists()
        {
            // Arrange & Act
            var programType = typeof(EasySave.Program);

            // Assert
            Assert.NotNull(programType);
        }

        [Fact]
        public void Program_HasMainMethod()
        {
            // Arrange
            var programType = typeof(EasySave.Program);

            // Act
            var mainMethod = programType.GetMethod("Main", 
                System.Reflection.BindingFlags.Static | 
                System.Reflection.BindingFlags.NonPublic);

            // Assert
            Assert.NotNull(mainMethod);
        }
    }
}