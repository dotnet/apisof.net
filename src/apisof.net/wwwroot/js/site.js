var siteConsent = null;

function setCookieManagementVisibility(isVisible) {
    var elements = document.querySelectorAll(".manageCookieChoice");
    for (var i = 0; i < elements.length; i++) {
        elements[i].style.display = isVisible ? "list-item" : "none";
    }
}

function initializeCookieConsent() {
    if (typeof WcpConsent === "undefined" || typeof WcpConsent.init !== "function") {
        return;
    }

    WcpConsent.init("en-US", "cookie-banner", function (err, consentInstance) {
        if (err || !consentInstance) {
            return;
        }

        siteConsent = consentInstance;
        setCookieManagementVisibility(!!siteConsent.isConsentRequired);

        if (typeof siteConsent.onConsentChanged === "function") {
            siteConsent.onConsentChanged(function () {
                setCookieManagementVisibility(!!siteConsent.isConsentRequired);
            });
        }
    });
}

function openCookiePreferences() {
    var consent = siteConsent || (typeof WcpConsent !== "undefined" ? WcpConsent.siteConsent : null);
    if (consent && typeof consent.manageConsent === "function") {
        consent.manageConsent();
    }
}

window.openCookiePreferences = openCookiePreferences;

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initializeCookieConsent);
} else {
    initializeCookieConsent();
}

function scrollIntoMainContent() {
    var mainContent = document.getElementById("main-content");
    if (mainContent) {
        mainContent.scrollIntoView({ behavior: "smooth" });
        mainContent.focus();
    }
}

var observer = new MutationObserver(function () {
    $('[data-toggle="popover"]').popover({
        placement: 'top',
        trigger: 'hover',
        boundary: 'body'
    });
    $('[data-toggle="popover"]').on('click', function () {
        $('[data-toggle="popover"]').popover('dispose');
    });
    $('.search-result-row.selected').each(function () {
        this.scrollIntoView({ block: "nearest" });
    });
    $("#skipToMain").on('keydown', function (e) {
        if (e.key === "Enter" || e.key === " ") {
            scrollIntoMainContent();
            e.preventDefault();
        }
    });
    $("#skipToMain").on('click', function (e) {
        scrollIntoMainContent();
        e.preventDefault();
    });
});

observer.observe(document, {
    subtree: true,
    childList: true,
    attributes: true
});
