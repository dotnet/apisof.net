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

function createCookiePreferencesControl() {
    var control = {
        preferencesDialog: null,
        backdrop: null,
        
        createDialog: function() {
            var self = this;
            
            // Create backdrop
            this.backdrop = document.createElement('div');
            this.backdrop.id = 'cookie-preferences-backdrop';
            this.backdrop.setAttribute('role', 'presentation');
            
            // Create dialog container
            this.preferencesDialog = document.createElement('div');
            this.preferencesDialog.id = 'cookie-preferences-dialog';
            this.preferencesDialog.setAttribute('role', 'dialog');
            this.preferencesDialog.setAttribute('aria-labelledby', 'cookie-preferences-title');
            this.preferencesDialog.setAttribute('aria-modal', 'true');
            
            // Create dialog content
            var content = document.createElement('div');
            content.className = 'cookie-preferences-content';
            
            // Header
            var header = document.createElement('div');
            header.className = 'cookie-preferences-header';
            var title = document.createElement('h2');
            title.id = 'cookie-preferences-title';
            title.textContent = 'Manage your consent preferences';
            header.appendChild(title);
            
            // Close button
            var closeBtn = document.createElement('button');
            closeBtn.className = 'cookie-preferences-close';
            closeBtn.setAttribute('aria-label', 'Close preferences dialog');
            closeBtn.innerHTML = '×';
            closeBtn.onclick = function() { self.hidePreferences(); };
            header.appendChild(closeBtn);
            
            content.appendChild(header);
            
            // Preferences sections
            var preferencesContainer = document.createElement('div');
            preferencesContainer.className = 'cookie-preferences-container';
            
            var categories = [
                { name: 'Required', category: WcpConsent.consentCategories.Required, description: 'Essential for website functionality' },
                { name: 'Analytics', category: WcpConsent.consentCategories.Analytics, description: 'Help us understand how you use our site' },
                { name: 'Social Media', category: WcpConsent.consentCategories.SocialMedia, description: 'Enable social media features' },
                { name: 'Advertising', category: WcpConsent.consentCategories.Advertising, description: 'Personalized advertising' }
            ];
            
            categories.forEach(function(cat) {
                var section = document.createElement('div');
                section.className = 'cookie-preference-section';
                
                var labelContainer = document.createElement('div');
                labelContainer.className = 'cookie-preference-label-container';
                
                var label = document.createElement('label');
                label.className = 'cookie-preference-label';
                
                var checkbox = document.createElement('input');
                checkbox.type = 'checkbox';
                checkbox.className = 'cookie-preference-checkbox';
                checkbox.dataset.category = cat.name;
                if (cat.name === 'Required') {
                    checkbox.disabled = true;
                    checkbox.checked = true;
                }
                
                var labelText = document.createElement('span');
                labelText.className = 'cookie-preference-name';
                labelText.textContent = cat.name;
                
                label.appendChild(checkbox);
                label.appendChild(labelText);
                
                var description = document.createElement('p');
                description.className = 'cookie-preference-description';
                description.textContent = cat.description;
                
                labelContainer.appendChild(label);
                labelContainer.appendChild(description);
                
                section.appendChild(labelContainer);
                preferencesContainer.appendChild(section);
            });
            
            content.appendChild(preferencesContainer);
            
            // Footer with buttons
            var footer = document.createElement('div');
            footer.className = 'cookie-preferences-footer';
            
            var rejectBtn = document.createElement('button');
            rejectBtn.className = 'cookie-preferences-button cookie-preferences-reject';
            rejectBtn.textContent = 'Reject All';
            rejectBtn.onclick = function() { self.rejectAll(); };
            
            var acceptBtn = document.createElement('button');
            acceptBtn.className = 'cookie-preferences-button cookie-preferences-accept';
            acceptBtn.textContent = 'Accept All';
            acceptBtn.onclick = function() { self.acceptAll(); };
            
            var saveBtn = document.createElement('button');
            saveBtn.className = 'cookie-preferences-button cookie-preferences-save';
            saveBtn.textContent = 'Save Preferences';
            saveBtn.onclick = function() { self.savePreferences(); };
            
            footer.appendChild(rejectBtn);
            footer.appendChild(acceptBtn);
            footer.appendChild(saveBtn);
            
            content.appendChild(footer);
            this.preferencesDialog.appendChild(content);
            
            // Add to body
            document.body.appendChild(this.backdrop);
            document.body.appendChild(this.preferencesDialog);
            
            // Handle escape key
            document.addEventListener('keydown', function(e) {
                if (e.key === 'Escape' && self.preferencesDialog && self.preferencesDialog.style.display !== 'none') {
                    self.hidePreferences();
                }
            });
        },
        
        showPreferences: function() {
            if (!this.preferencesDialog) {
                this.createDialog();
            }
            this.backdrop.style.display = 'block';
            this.preferencesDialog.style.display = 'block';
            // Load current preferences
            this.loadPreferences();
        },
        
        hidePreferences: function() {
            if (this.backdrop) this.backdrop.style.display = 'none';
            if (this.preferencesDialog) this.preferencesDialog.style.display = 'none';
        },
        
        loadPreferences: function() {
            if (!siteConsent) return;
            var checkboxes = document.querySelectorAll('.cookie-preference-checkbox');
            checkboxes.forEach(function(checkbox) {
                var category = checkbox.dataset.category;
                if (category === 'Required') {
                    checkbox.checked = true;
                } else {
                    checkbox.checked = siteConsent.getConsentFor(WcpConsent.consentCategories[category]);
                }
            });
        },
        
        acceptAll: function() {
            if (!siteConsent) return;
            siteConsent.setConsent(true);
            this.hidePreferences();
        },
        
        rejectAll: function() {
            if (!siteConsent) return;
            var preferences = {
                Required: true,
                Analytics: false,
                SocialMedia: false,
                Advertising: false
            };
            siteConsent.setConsent(preferences);
            this.hidePreferences();
        },
        
        savePreferences: function() {
            if (!siteConsent) return;
            var preferences = {};
            var checkboxes = document.querySelectorAll('.cookie-preference-checkbox');
            checkboxes.forEach(function(checkbox) {
                preferences[checkbox.dataset.category] = checkbox.checked;
            });
            siteConsent.setConsent(preferences);
            this.hidePreferences();
        }
    };
    
    return control;
}

function manageConsent() {
    if (typeof window.cookiePreferencesControl === 'undefined') {
        window.cookiePreferencesControl = createCookiePreferencesControl();
    }
    if (window.cookiePreferencesControl) {
        window.cookiePreferencesControl.showPreferences();
    }
}

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
