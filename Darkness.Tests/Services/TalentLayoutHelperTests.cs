using Darkness.Core.Models;
using Darkness.Core.Services;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Darkness.Tests.Services
{
    public class TalentLayoutHelperTests
    {
        [Fact]
        public void CalculateLayout_SingleRoot_CentersInColumn1()
        {
            var nodes = new List<TalentNode>
            {
                new TalentNode { Id = "root", Name = "Root" }
            };

            TalentLayoutHelper.CalculateLayout(nodes);

            Assert.Equal(0, nodes[0].Row);
            Assert.Equal(1, nodes[0].Column);
        }

        [Fact]
        public void CalculateLayout_ParentAndChild_AlignInColumn1()
        {
            var nodes = new List<TalentNode>
            {
                new TalentNode { Id = "root", Name = "Root" },
                new TalentNode { Id = "child", Name = "Child", PrerequisiteNodeIds = { "root" } }
            };

            TalentLayoutHelper.CalculateLayout(nodes);

            Assert.Equal(0, nodes[0].Row);
            Assert.Equal(1, nodes[0].Column);
            Assert.Equal(1, nodes[1].Row);
            Assert.Equal(1, nodes[1].Column);
        }

        [Fact]
        public void CalculateLayout_OneParentTwoChildren_BranchesOut()
        {
            var nodes = new List<TalentNode>
            {
                new TalentNode { Id = "root", Name = "Root" },
                new TalentNode { Id = "child1", Name = "Child 1", PrerequisiteNodeIds = { "root" } },
                new TalentNode { Id = "child2", Name = "Child 2", PrerequisiteNodeIds = { "root" } }
            };

            TalentLayoutHelper.CalculateLayout(nodes);

            Assert.Equal(1, nodes[0].Column);
            // Children should be at 0 and 2
            var columns = new List<int> { nodes[1].Column, nodes[2].Column };
            Assert.Contains(0, columns);
            Assert.Contains(2, columns);
        }

        [Fact]
        public void CalculateLayout_Convergence_CentersInColumn1()
        {
            var nodes = new List<TalentNode>
            {
                new TalentNode { Id = "root1", Name = "Root 1" },
                new TalentNode { Id = "root2", Name = "Root 2" },
                new TalentNode { Id = "converged", Name = "Converged", PrerequisiteNodeIds = { "root1", "root2" } }
            };

            TalentLayoutHelper.CalculateLayout(nodes);

            Assert.Equal(1, nodes[2].Column);
            Assert.Equal(1, nodes[2].Row);
        }

        [Fact]
        public void CalculateLayout_LongPath_SetsRowCorrectly()
        {
            var nodes = new List<TalentNode>
            {
                new TalentNode { Id = "root", Name = "Root" },
                new TalentNode { Id = "mid1", Name = "Mid 1", PrerequisiteNodeIds = { "root" } },
                new TalentNode { Id = "mid2", Name = "Mid 2", PrerequisiteNodeIds = { "root" } },
                new TalentNode { Id = "end", Name = "End", PrerequisiteNodeIds = { "mid1", "mid2" } }
            };

            TalentLayoutHelper.CalculateLayout(nodes);

            Assert.Equal(0, nodes[0].Row);
            Assert.Equal(1, nodes[1].Row);
            Assert.Equal(1, nodes[2].Row);
            Assert.Equal(2, nodes[3].Row);
        }
    }
}
