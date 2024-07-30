using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using PostItter_RESTfulAPI.Models.DatabaseModels;

namespace PostItter_RESTfulAPI.DatabaseContext;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<UserDto> users { get; set; }
    public DbSet<PostDto> posts { get; set; }
    public DbSet<CommentDto> comments { get; set; }
    public DbSet<NotificationDto> notifications { get; set; }
    public DbSet<BlockedUserDTO> blockedUsers { get; set; }
    public DbSet<HashtagDto> hashtags { get; set; }
    public DbSet<LikeDto> likes { get; set; }
    public DbSet<UserConnectionDto> connections { get; set; }
}