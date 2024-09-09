using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using PostItter_RESTfulAPI.Entity.DatabaseModels;

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
    public DbSet<UserSettingsDto> settings { get; set; }
    public DbSet<ChatDto> chats { get; set; }
    public DbSet<MessageDto> messages { get; set; }
    public DbSet<ActiveUsersDto> activeUsers { get; set; }
    public DbSet<PasswordRecoveryTokenDto> passwordRecoveryTokens { get; set; }
}