/*
 *       _            _                       _
 *  __ _| |_  _ __   /_\   ____  _ _ _  __   (_)___
 * / _` | ' \| '  \ / _ \ (_-< || | ' \/ _|_ | (_-<
 * \__, |_||_|_|_|_/_/ \_\/__/\_, |_||_\__(_)/ /__/
 * |___/                      |__/         |__/
 */

/**
 * GHM Form Handler for WebForms
 */
class GHMFormHandler extends BaseFormHandler {
    /**
     * Initializes a new instance of the GHMFormHandler class.
     */
    constructor() {
        super();

        // Bind methods to preserve 'this' context
        if (this.handlePublicationSelection != null) this.handlePublicationSelection = this.handlePublicationSelection.bind(this);
    }

    /**
     * Initializes event listeners for the form elements.
     */
    initializeEventListeners() {
        const ddlPublication = document.getElementById('ddlPublication');
        if (ddlPublication) {
            ddlPublication.addEventListener('change', async (e) => {
                await this.handlePublicationSelection(e.target.value);
            });
        }

        const ddlOffers = document.getElementById('ddlOffers');
        if (ddlOffers) {
            ddlOffers.addEventListener('change', async (e) => {
                await this.handleOfferSelection(e.target.value);
            });
        }

        const txtCC = document.getElementById('txtCC');
        if (txtCC) {
            txtCC.addEventListener('blur', async (e) => {
                const requireCC = document.getElementById('hdnRequireCC');
                if (!requireCC)
                    return;

                const offerId = document.getElementById('hdnOfferID').value;
                if (offerId) {
                    await this.handleOfferSelection(offerId);
                }
            });
        }

        const chkDeliveryCredit = document.getElementById('chkDeliveryCredit');
        const txtDeliveryCredit = document.getElementById('txtDeliveryCredit');
        if (txtDeliveryCredit && chkDeliveryCredit) {
            txtDeliveryCredit.addEventListener('keydown', CheckNumericWithDecimal);
            txtDeliveryCredit.addEventListener('blur', (e) => {
                var val = (e.target.value ? parseFloat(e.target.value) : 0);
                val = Math.min(val, 40);
                chkDeliveryCredit.checked = val > 0;
                e.target.value = val.toFixed(2);
            });
            console.log('Envent listeners attached to txtWaivedAmount');
        }
    }

    /**
     * Handles the selection of a publication.
     * @param {string} publicationId - The ID of the selected publication.
     * @param {boolean} [isLoading=false] - Indicates whether the page is currently loading.
     */
    async handlePublicationSelection(publicationId, isLoading = false) {
        /*
         * Get the dropdown elements
         */
        let hdnPubId = document.getElementById('hdnPubId');
        let ddlPublication = document.getElementById('ddlPublication');
        let ddlOffers = document.getElementById('ddlOffers');
        if (!ddlPublication || !ddlOffers)
            return;

        if (hdnPubId)
            hdnPubId.value = publicationId;

        if (publicationId == "") {
            ddlOffers.value = "";
            ddlOffers.innerHTML = "";
            this.handleOfferSelection("");
            return;
        }

        let recordType = 'WINBACK';
        let lblRecordTypeText = document.getElementById('lblRecordType').innerText;
        if (lblRecordTypeText != 'N/A')
            recordType = lblRecordTypeText;

        /*
         * Get the offers for the selected publication
         */
        await this.executeOperation(async () => {
            try {
                let isRetention = recordType.toLowerCase() === 'retention';
                let stopFlag = document.getElementById('hdnStopFlag').value.toLowerCase() === "true";
                await new Promise((resolve, reject) => {
                    PageMethods.GetOffersByPublication(
                        recordType,
                        publicationId,
                        stopFlag,
                        isRetention,
                        (result) => resolve(result),
                        (error) => reject(error)
                    );
                }).then(this.handleOffersLoad);
            } catch (error) {
                console.error('Error loading offers:', error);
                this.displayZipError();
                throw error;
            }
        });
    }

    /**
     * Handles the loaded offers result.
     * @param {string} result - The result of the offers load.
     */
    handleOffersLoad(result) {
        const offers = JSON.parse(result);

        if (!offers || offers.length === 0) {
            this.displayZipError();
            return;
        }

        // Cache the offers
        this.offersCache.clear();
        offers.forEach(offer => {
            this.offersCache.set(offer.OfferId, offer);
        });

        this.populateOffersDropdown(offers);

        // Only select first offer if we have offers
        if (offers.length > 0 && window.location.href.toLowerCase().indexOf("in.aspx") == -1) {
            this.handleOfferSelection(offers[0].OfferId);
        }
    }

    /**
     * Handles the selection of an offer.
     * @param {string} offerId - The ID of the selected offer.
     * @param {boolean} [isLoading=false] - Indicates whether the page is currently loading.
     */
    async handleOfferSelection(offerId, isLoading = false) {
        if (offerId === "") {
            this.RequireValue("txtCC", false);
            this.RequireValue("txtCCExpDate", false);
            this.RequireValue("txtBillingAddress", false);
            this.RequireValue("txtBillingAddress2", false);
            this.RequireValue("txtPaymentAmount", false);
            this.RequireValue("txtStartDate", false);

            document.getElementById('txtStartDate').value = "";
            document.getElementById('txtPaymentAmount').value = "";
            document.getElementById('lblScriptText').innerHTML = "";
            document.getElementById('lblRecapText').innerHTML = "";
            return;
        }

        document.getElementById('txtStartDate').setAttribute('loading', (isLoading ? 'true' : 'false'));

        let recordType = 'WINBACK';
        let lblRecordTypeText = document.getElementById('lblRecordType').innerText;
        if (lblRecordTypeText != 'N/A')
            recordType = lblRecordTypeText;

        await this.executeOperation(async () => {
            try {
                // Check if we have the offer details cached
                if (this.offersCache.has(offerId)) {
                    this.handleOfferDetailsLoad(JSON.stringify(this.offersCache.get(offerId)));
                    return;
                }

                // If not in cache, fetch from server
                let isRetention = recordType.toLowerCase() === 'retention';
                await new Promise((resolve, reject) => {
                    const recordID = Number(document.getElementById('hdnRecordID').value);
                    if (recordID > 0) {
                        PageMethods.GetOfferDetailsForRecord(
                            recordID,
                            Number(offerId),
                            (result) => resolve(result),
                            (error) => reject(error)
                        );
                    } else {
                        PageMethods.GetOfferDetails(
                            offerId,
                            isRetention,
                            (result) => resolve(result),
                            (error) => reject(error)
                        );
                    }
                }).then(this.handleOfferDetailsLoad);
            } catch (error) {
                console.error('Error loading offer details:', error);
                throw error;
            }
        });
    }

    /**
     * Extends base processTemplate to add GHM-specific replacements.
     * @param {string} template - The template to process.
     * @param {Object} offer - The offer details.
     * @returns {string} The processed template.
     */
    processTemplate(template, offer) {
        // Clear any existing entries
        this.dictionaryProcessor.clear();

        var isWireless = document.getElementById('hdnIsWireless').value === "true";
        var lastName = document.getElementById('txtFullName').value.split(' ').slice(-1)[0];

        // Add GHM-specific replacements
        this.dictionaryProcessor.add('Agent First', document.getElementById('hdnAgentName').value);
        this.dictionaryProcessor.add('Amount', offer.PerTermPrice.toFixed(2));
        this.dictionaryProcessor.add('Customer Service', offer.PhoneNumber);
        this.dictionaryProcessor.add('Delivery Schedule', offer.DeliverySchedule);
        this.dictionaryProcessor.add('Paper Name', offer.PaperName);
        const originalPublication = document.getElementById('lblPublication');
        this.dictionaryProcessor.add('Original Publication', originalPublication ? originalPublication.innerHTML : '');
        this.dictionaryProcessor.add('Payment Type', (offer.CreditCardRequired ? "EZPay" : "Billed"));
        this.dictionaryProcessor.add('Term', offer.Term);
        this.dictionaryProcessor.add('Wireless', isWireless ? `Hello, is this Mr./Mrs. ${lastName}` : '');

        var i = 1;
        var hdn = document.getElementById(`hdnUserDefined${i}`);;
        do {
            if (hdn)
                this.dictionaryProcessor.add(`User Defined ${i}`, hdn.value);
            hdn = document.getElementById(`hdnUserDefined${++i}`);
        } while (hdn);

        // Process the template
        let processed = this.dictionaryProcessor.process(template);

        // Handle line breaks
        return processed.replace(/\n/g, '\n<br/>');
    }
}

/**
 * Changes the display property of the .retention-data CSS rule in ghmStyle.css.
 */
function changeRetentionDataDisplay(isRetention) {
    const stylesheets = document.styleSheets;
    for (let i = 0; i < stylesheets.length; i++) {
        const sheet = stylesheets[i];
        // Check if this is the ghmStyle.css stylesheet
        if (sheet.href && sheet.href.includes('ghmStyle.css')) {
            const rules = sheet.cssRules || sheet.rules; // Handle browser differences
            for (let j = 0; j < rules.length; j++) {
                const rule = rules[j];
                if (rule.selectorText === '.retention-data') {
                    rule.style.display = isRetention ? 'block' : 'none';
                }
                if (rule.selectorText === '.no-retention') {
                    rule.style.display = isRetention ? 'none' : 'block';
                }
            }
        }
    }
}

// Initialize the form handler when the document is ready
document.addEventListener('DOMContentLoaded', () => {
    window.ghmForm = new GHMFormHandler();

    // Enable auto-save functionality
    window.ghmForm.enableAutoSave();

    // If record type is retention, show retention data sections
    var recordType = document.getElementById('hdnRecordType').value;
    changeRetentionDataDisplay(recordType && recordType.toLowerCase() == 'retention');

    var preload = document.getElementById('hdnIsOfferPreload');
    if (preload && preload.value == "true") {
        preload.value = "false";
        return;
    }

    var offerID = document.getElementById('hdnOfferID').value;
    if (offerID) {
        window.ghmForm.handleOfferSelection(offerID, true);
    };
});
