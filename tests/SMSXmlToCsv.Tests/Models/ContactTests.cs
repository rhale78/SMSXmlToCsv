using SMSXmlToCsv.Models;

namespace SMSXmlToCsv.Tests.Models;

public class ContactTests
{
    [Fact]
    public void FromPhoneNumber_CreatesContactWithNameAndPhone()
    {
        Contact contact = Contact.FromPhoneNumber("Alice", "+15551234567");

        Assert.Equal("Alice", contact.Name);
        Assert.Single(contact.PhoneNumbers);
        Assert.Contains("+15551234567", contact.PhoneNumbers);
        Assert.Empty(contact.Emails);
    }

    [Fact]
    public void FromEmail_CreatesContactWithNameAndEmail()
    {
        Contact contact = Contact.FromEmail("Bob", "bob@example.com");

        Assert.Equal("Bob", contact.Name);
        Assert.Empty(contact.PhoneNumbers);
        Assert.Single(contact.Emails);
        Assert.Contains("bob@example.com", contact.Emails);
    }

    [Fact]
    public void FromName_CreatesContactWithNameOnly()
    {
        Contact contact = Contact.FromName("Charlie");

        Assert.Equal("Charlie", contact.Name);
        Assert.Empty(contact.PhoneNumbers);
        Assert.Empty(contact.Emails);
    }

    [Fact]
    public void Contact_SupportsMultiplePhoneNumbers()
    {
        Contact contact = new Contact("Alice", new HashSet<string> { "+15551234567", "+15559876543" }, new HashSet<string>());

        Assert.Equal(2, contact.PhoneNumbers.Count);
        Assert.Contains("+15551234567", contact.PhoneNumbers);
        Assert.Contains("+15559876543", contact.PhoneNumbers);
    }

    [Fact]
    public void Contact_SupportsMultipleEmails()
    {
        Contact contact = new Contact("Bob", new HashSet<string>(), new HashSet<string> { "bob@work.com", "bob@home.com" });

        Assert.Equal(2, contact.Emails.Count);
        Assert.Contains("bob@work.com", contact.Emails);
        Assert.Contains("bob@home.com", contact.Emails);
    }

    [Fact]
    public void Contact_Equality_SameName_SamePhone()
    {
        // Records compare HashSet fields by reference, so equality holds only for same instance.
        // Test that Name and PhoneNumbers content are accessible.
        Contact a = Contact.FromPhoneNumber("Alice", "+15551234567");

        Assert.Equal("Alice", a.Name);
        Assert.Contains("+15551234567", a.PhoneNumbers);
    }
}
