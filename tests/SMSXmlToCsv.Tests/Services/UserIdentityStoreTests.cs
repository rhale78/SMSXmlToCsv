using SMSXmlToCsv.Services;

namespace SMSXmlToCsv.Tests.Services;

public class UserIdentityStoreTests : IDisposable
{
    // Reset the store before/after each test by clearing via reflection since there's no public Clear()
    public UserIdentityStoreTests()
    {
        ClearStore();
    }

    public void Dispose()
    {
        ClearStore();
    }

    private static void ClearStore()
    {
        // Access private fields via reflection to clear for test isolation
        Type type = typeof(UserIdentityStore);
        System.Reflection.FieldInfo? namesField = type.GetField("_names",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Reflection.FieldInfo? emailsField = type.GetField("_emails",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        (namesField?.GetValue(null) as HashSet<string>)?.Clear();
        (emailsField?.GetValue(null) as HashSet<string>)?.Clear();
    }

    [Fact]
    public void AddName_StoresName()
    {
        UserIdentityStore.AddName("Alice");

        Assert.Contains("Alice", UserIdentityStore.Names);
    }

    [Fact]
    public void AddName_IgnoresNullOrWhitespace()
    {
        UserIdentityStore.AddName(null);
        UserIdentityStore.AddName("   ");

        Assert.Empty(UserIdentityStore.Names);
    }

    [Fact]
    public void AddEmail_StoresValidEmail()
    {
        UserIdentityStore.AddEmail("alice@example.com");

        Assert.Contains("alice@example.com", UserIdentityStore.Emails);
    }

    [Fact]
    public void AddEmail_IgnoresInvalidEmail()
    {
        UserIdentityStore.AddEmail("not-an-email");

        Assert.Empty(UserIdentityStore.Emails);
    }

    [Fact]
    public void AddEmail_IgnoresNullOrWhitespace()
    {
        UserIdentityStore.AddEmail(null);
        UserIdentityStore.AddEmail("  ");

        Assert.Empty(UserIdentityStore.Emails);
    }

    [Fact]
    public void AddNames_StoresMultipleNames()
    {
        UserIdentityStore.AddNames(new[] { "Alice", "Bob", "Charlie" });

        Assert.Equal(3, UserIdentityStore.Names.Count);
        Assert.Contains("Alice", UserIdentityStore.Names);
        Assert.Contains("Bob", UserIdentityStore.Names);
        Assert.Contains("Charlie", UserIdentityStore.Names);
    }

    [Fact]
    public void AddEmails_StoresMultipleValidEmails()
    {
        UserIdentityStore.AddEmails(new[] { "alice@example.com", "bob@example.com" });

        Assert.Equal(2, UserIdentityStore.Emails.Count);
    }

    [Fact]
    public void AddNames_IsNullSafe()
    {
        UserIdentityStore.AddNames(null!);

        Assert.Empty(UserIdentityStore.Names);
    }

    [Fact]
    public void AddEmails_IsNullSafe()
    {
        UserIdentityStore.AddEmails(null!);

        Assert.Empty(UserIdentityStore.Emails);
    }

    [Fact]
    public void Names_AreCaseInsensitiveForDuplicates()
    {
        UserIdentityStore.AddName("Alice");
        UserIdentityStore.AddName("alice");

        // HashSet with OrdinalIgnoreCase should deduplicate
        Assert.Single(UserIdentityStore.Names);
    }
}
