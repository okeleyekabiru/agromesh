using System.Net;
using System.Text;
using Ilevora.Application.Interfaces;

namespace Ilevora.Application.Email;

/// <summary>
/// Ilevora-branded transactional emails (HTML plus plain text). Email clients rarely load web fonts, so Plus Jakarta Sans
/// is only the first choice in a system font stack. The farm logo is deliberately never used here (it belongs on invoices).
/// </summary>
public static class EmailTemplates
{
    private const string Green = "#138A44";
    // The brand guide flags #138A44 with white small text as just under AA, so buttons use a slightly deeper green.
    private const string ButtonGreen = "#0F7A3A";
    private const string Font = "'Plus Jakarta Sans','Segoe UI',Roboto,Helvetica,Arial,sans-serif";

    public static EmailMessage Invitation(string to, string inviterName, string farmName, string roleLabel, string link, int validDays)
    {
        var subject = $"{inviterName} invited you to join {farmName} on Ilevora";
        var html = Layout(farmName, "You have been invited",
            $"<p>{H(inviterName)} invited you to join <strong>{H(farmName)}</strong> on Ilevora as <strong>{H(roleLabel)}</strong>.</p>" +
            "<p>Set a password to accept the invitation and get started.</p>",
            "Accept invitation", link,
            $"This link works once and expires in {validDays} days. If you were not expecting this, you can ignore this email.");
        var text = $"{inviterName} invited you to join {farmName} on Ilevora as {roleLabel}.\n\nAccept the invitation and set your password:\n{link}\n\nThis link works once and expires in {validDays} days. If you were not expecting this, you can ignore this email.\n";
        return new EmailMessage(to, subject, html, text);
    }

    public static EmailMessage PasswordReset(string to, string name, string link, int validMinutes)
    {
        var html = Layout(null, "Reset your password",
            $"<p>Hi {H(name)},</p><p>We received a request to reset your Ilevora password.</p>",
            "Choose a new password", link,
            $"This link works once and expires in {validMinutes} minutes. If you did not ask for this, you can safely ignore this email; your password will not change.");
        var text = $"Hi {name},\n\nWe received a request to reset your Ilevora password. Choose a new one here:\n{link}\n\nThis link works once and expires in {validMinutes} minutes. If you did not ask for this, ignore this email and your password will not change.\n";
        return new EmailMessage(to, "Reset your Ilevora password", html, text);
    }

    public static EmailMessage PasswordChanged(string to, string name)
    {
        var html = Layout(null, "Your password was changed",
            $"<p>Hi {H(name)},</p><p>The password on your Ilevora account was just changed.</p>",
            null, null,
            "If this was not you, reset your password straight away and tell your farm admin.");
        var text = $"Hi {name},\n\nThe password on your Ilevora account was just changed.\n\nIf this was not you, reset your password straight away and tell your farm admin.\n";
        return new EmailMessage(to, "Your Ilevora password was changed", html, text);
    }

    public static EmailMessage Welcome(string to, string name, string loginUrl)
    {
        var html = Layout(null, "Welcome to Ilevora",
            $"<p>Hi {H(name)},</p><p>Your account is ready. Ilevora keeps your blocks, work, stock and money in one place.</p>",
            "Open Ilevora", loginUrl, "You are receiving this because an account was created with this email address.");
        var text = $"Hi {name},\n\nYour account is ready. Open Ilevora:\n{loginUrl}\n\nYou are receiving this because an account was created with this email address.\n";
        return new EmailMessage(to, "Welcome to Ilevora", html, text);
    }

    /// <summary>Optional: tells the person who invited someone that the invitation was accepted.</summary>
    public static EmailMessage InvitationAccepted(string to, string inviterName, string newPersonName, string roleLabel)
    {
        var html = Layout(null, $"{newPersonName} joined your farm",
            $"<p>Hi {H(inviterName)},</p><p><strong>{H(newPersonName)}</strong> accepted your invitation and joined as <strong>{H(roleLabel)}</strong>.</p>",
            null, null, "You can turn off optional emails like this one in your Ilevora settings.");
        var text = $"Hi {inviterName},\n\n{newPersonName} accepted your invitation and joined as {roleLabel}.\n\nYou can turn off optional emails like this one in your Ilevora settings.\n";
        return new EmailMessage(to, $"{newPersonName} joined your farm on Ilevora", html, text);
    }

    private static string H(string s) => WebUtility.HtmlEncode(s);

    private static string Layout(string? context, string heading, string bodyHtml, string? buttonLabel, string? link, string footer)
    {
        var sb = new StringBuilder();
        sb.Append($"<!doctype html><html><body style=\"margin:0;background:#F4F7F5;font-family:{Font};color:#0D2B1F;\">");
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr><td align=\"center\" style=\"padding:24px 12px;\">");
        sb.Append("<table role=\"presentation\" width=\"560\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:560px;background:#ffffff;border-radius:12px;overflow:hidden;\">");
        sb.Append($"<tr><td style=\"background:{Green};padding:20px 28px;color:#ffffff;font-size:22px;font-weight:800;letter-spacing:0.01em;\">Ilevora</td></tr>");
        sb.Append("<tr><td style=\"padding:28px;font-size:15px;line-height:1.6;\">");
        sb.Append($"<h1 style=\"margin:0 0 16px;font-size:22px;line-height:1.3;\">{H(heading)}</h1>");
        sb.Append(bodyHtml);
        if (buttonLabel is not null && link is not null)
        {
            var safe = H(link);
            sb.Append($"<p style=\"margin:24px 0;\"><a href=\"{safe}\" style=\"background:{ButtonGreen};color:#ffffff;text-decoration:none;font-weight:700;padding:12px 22px;border-radius:8px;display:inline-block;\">{H(buttonLabel)}</a></p>");
            sb.Append($"<p style=\"font-size:12px;color:#5b6b63;\">Button not working? Paste this link into your browser:<br><a href=\"{safe}\" style=\"color:{ButtonGreen};word-break:break-all;\">{safe}</a></p>");
        }
        sb.Append($"<p style=\"margin-top:24px;font-size:12px;color:#5b6b63;\">{H(footer)}</p>");
        sb.Append("</td></tr></table></td></tr></table></body></html>");
        return sb.ToString();
    }
}
