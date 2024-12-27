using Microsoft.EntityFrameworkCore;
using Reddit.Models;
using Reddit.Repositories;

namespace Reddit.UnitTest.Kumi
{
    public class PostRepositoryTests
    {
        private IPostsRepository GetRepositoryWithTestData()
        {
            var dbName = Guid.NewGuid().ToString(); // Unique database name per test
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var dbContext = new ApplicationDbContext(options);

            dbContext.Posts.AddRange(
                new Post { Title = "First Post", Content = "Content 1", Upvote = 5, Downvote = 1 }, // 5/6 -1
                new Post { Title = "Second Post", Content = "Some Other Content", Upvote = 12, Downvote = 1 } ,
                new Post { Title = "Third Post", Content = "Content 1", Upvote = 3, Downvote = 1 },
                new Post { Title = "Popular Post", Content = "Content 1", Upvote = 221, Downvote = 1 },
                new Post { Title = "Controversial Post", Content = "Content 1", Upvote = 5, Downvote = 2123 } // 2
                // (post.Upvote) / (post.Upvote + post.Downvote)
            );

            dbContext.SaveChanges();
            return new PostsRepository(dbContext);
        }

        [Fact]
        public async Task GetPosts_ReturnsPaginatedPosts()
        {
            var postsRepository = GetRepositoryWithTestData();

            var pagedPosts = await postsRepository.GetPosts(1, 3, null, null, true);

            Assert.Equal(3, pagedPosts.Items.Count()); // Validate pagination
        }

        [Fact]
        public async Task GetPosts_FiltersBySearchTerm()
        {
            var postsRepository = GetRepositoryWithTestData();

            var pagedPosts = await postsRepository.GetPosts(1, 10, "Second", null, true);

            Assert.Single(pagedPosts.Items);
            Assert.Equal("Second Post", pagedPosts.Items.First().Title); // Validate filtering
        }

        [Fact]
        public async Task GetPosts_SortsByPositivity()
        {
            var postsRepository = GetRepositoryWithTestData();

            var pagedPosts = await postsRepository.GetPosts(1, 5, null, "positivity", false); // (post.Upvote) / (post.Upvote + post.Downvote)

            var expectedOrder = new[] { "Popular Post", "Second Post", "First Post", "Third Post", "Controversial Post" };
            Assert.Equal(expectedOrder, pagedPosts.Items.Select(p => p.Title).ToArray());
        }

        [Fact]
        public async Task TestAnri()
        {
            var posts = new List<Post>
{
    new Post { Title = "First Post", Content = "Content 1", Upvote = 5, Downvote = 1 }, // 5 / 6 ≈ 0.8333
    new Post { Title = "Second Post", Content = "Some Other Content", Upvote = 12, Downvote = 1 }, // 12 / 13 ≈ 0.9231
    new Post { Title = "Third Post", Content = "Content 1", Upvote = 3, Downvote = 1 }, // 3 / 4 = 0.75
    new Post { Title = "Popular Post", Content = "Content 1", Upvote = 221, Downvote = 1 }, // 221 / 222 ≈ 0.9955
    new Post { Title = "Controversial Post", Content = "Content 1", Upvote = 5, Downvote = 2123 } // 5 / 2128 ≈ 0.0023
};

            var sortedTitles = posts
                .OrderByDescending(p => (double)p.Upvote / (p.Upvote + p.Downvote))
                .Select(p => p.Title)
                .ToList();

         
                Console.WriteLine("");
        }

        [Fact]
        public async Task GetPosts_SortsByPopularity()
        {
            var postsRepository = GetRepositoryWithTestData();

            var pagedPosts = await postsRepository.GetPosts(1, 5, null, "popular", false);

            var expectedOrder = new[] { "Popular Post",  "Second Post", "First Post", "Third Post" , "Controversial Post" };
            Assert.Equal(expectedOrder, pagedPosts.Items.Select(p => p.Title).ToArray());
        }

        [Fact]
        public async Task GetPosts_ReturnsEmptyWhenNoMatch()
        {
            var postsRepository = GetRepositoryWithTestData();

            var pagedPosts = await postsRepository.GetPosts(1, 5, "Nonexistent", null, true);

            Assert.Empty(pagedPosts.Items); // Validate no matches
        }

        [Fact]
        public async Task GetPosts_HandlesEmptyDatabase()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var dbContext = new ApplicationDbContext(options);
            var postsRepository = new PostsRepository(dbContext);

            var pagedPosts = await postsRepository.GetPosts(1, 5, null, null, true);

            Assert.Empty(pagedPosts.Items); // Validate empty database
        }

        [Fact]
        public async Task GetPosts_HandlesInvalidSortTerm()
        {
            var postsRepository = GetRepositoryWithTestData();

            var pagedPosts = await postsRepository.GetPosts(1, 5, null, "invalidSortTerm", true);

            var expectedOrder = new[] { "First Post", "Second Post", "Third Post", "Popular Post", "Controversial Post" };
            Assert.Equal(expectedOrder, pagedPosts.Items.Select(p => p.Title).ToArray());
        }
    }
}
