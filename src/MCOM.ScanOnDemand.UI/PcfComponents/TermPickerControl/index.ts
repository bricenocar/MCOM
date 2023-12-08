import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { IInputs, IOutputs } from "./generated/ManifestTypes";
import { Termpicker } from './components/Termpicker';
import { ITermInfo } from '@pnp/sp/taxonomy';
import { stringOutPutSchema, termsOutPutSchema } from '../utilities/schemas';
import { SPTaxonomyService } from './services/SPTaxonomyService';
import { Optional } from './controls/modernTaxonomyPicker';
import { areValidGuids, getTermValuesArray, isValidGuid, isValidUrl, validTermValues } from '../utilities/common';
import { LanguageService } from '../services/languageService';
import { SPService } from '../services/spService';

export class TermPickerControl implements ComponentFramework.StandardControl<IInputs, IOutputs> {

    private initialValues: Optional<ITermInfo, "childrenCount" | "createdDateTime" | "lastModifiedDateTime" | "descriptions" | "customSortOrder" | "properties" | "localProperties" | "isDeprecated" | "isAvailableForTagging" | "topicRequested">[];
    private termValues: string; // type (ITermInfo) or Array in case the whole object is needed
    private terms: { id: string, labels: { isDefault: boolean, languageTag: string, name: string }[] }[];
    private termSetId: string;
    private anchorTermId: string;
    private extraAnchorTermIds: string;
    private notifyOutputChanged: () => void;
    private container: HTMLDivElement;
    private context: ComponentFramework.Context<IInputs>;
    private taxonomyService: SPTaxonomyService;
    private previousTermValues: string;
    private checkService: boolean;


    constructor() {
    }

    /**
     * Used to initialize the control instance. Controls can kick off remote server calls and other initialization actions here.
     * Data-set values are not initialized here, use updateView.
     * @param context The entire property bag available to control via Context Object; It contains values as set up by the customizer mapped to property names defined in the manifest, as well as utility functions.
     * @param notifyOutputChanged A callback method to alert the framework that the control has new outputs ready to be retrieved asynchronously.
     * @param state A piece of data that persists in one session for a single user. Can be set at any point in a controls life cycle by calling 'setControlState' in the Mode interface.
     * @param container If a control is marked control-type='standard', it will receive an empty div element within which it can render its content.
     */
    public async init(context: ComponentFramework.Context<IInputs>, notifyOutputChanged: () => void, state: ComponentFramework.Dictionary, container: HTMLDivElement): Promise<void> {
        // Add control initialization code
        this.notifyOutputChanged = notifyOutputChanged;
        this.container = container;
        this.context = context;
        this.context.mode.trackContainerResize(true);

        // Get the user's locale from userSettings. Not getting locale property in current version...
        const userLocale = (context.userSettings as any).locale || 'en-us';
        // Initialize LanguageService with the user's locale
        const languageService = LanguageService.getInstance();
        // Set the default language
        languageService.initializeLocale(userLocale);

        // Get the SPO service URL from the configuration
        const spoServiceUrl = context.parameters.ApiUrl.raw;
        // Initialize the SPService instance
        SPService.initialize(spoServiceUrl);
    }

    /**
     * Called when any value in the property bag has changed. This includes field values, data-sets, global values such as container height and width, offline status, control metadata values such as label, visible, etc.
     * @param context The entire property bag available to control via Context Object; It contains values as set up by the customizer mapped to names defined in the manifest, as well as utility functions
     */
    public async updateView(context: ComponentFramework.Context<IInputs>): Promise<void> {

        // storing the latest context and terms
        this.context = context;
        this.termSetId = context.parameters.TermSetId.raw;
        this.anchorTermId = context.parameters.AnchorTermId.raw;
        this.extraAnchorTermIds = context.parameters.ExtraAnchorTermIds.raw;

        // Get the current value of the property
        const apiUrl = context.parameters.ApiUrl.raw;
        const siteUrl = context.parameters.SiteUrl.raw;
        const currentTermValues = context.parameters.TermValues.raw || '';
        const areTermValuesValid = validTermValues(currentTermValues);

        // Validate terms
        const validApiUrl = isValidUrl(apiUrl);
        const validSiteUrl = isValidUrl(siteUrl);
        const validTermSetId = isValidGuid(this.termSetId);
        const validAnchorTermId = this.anchorTermId ? isValidGuid(this.anchorTermId) : true;
        const validExtraAnchorTermIds = this.extraAnchorTermIds ? areValidGuids(this.extraAnchorTermIds) : true;

        // Check if the termValues have changed
        if (currentTermValues !== this.previousTermValues) {
            // Update the previous value for the next check
            this.previousTermValues = currentTermValues;

            // Set initial term values in case is valid
            if (areTermValuesValid) {
                this.initialValues = getTermValuesArray(currentTermValues); // Pick values
            } else {
                this.initialValues = undefined; // Empty object
            }
        }

        // Get taxonomy service        
        if (!this.taxonomyService) {
            // Check if the SPO service URL is defined
            const spService = SPService.getInstance();
            if (!spService.getSpoServiceUrl()) {
                // Get the SPO service URL from the configuration

                // Initialize the SPService instance with the URL
                SPService.initialize(apiUrl);
            }

            this.checkService = await spService.serviceStatusCheck();
            if (validSiteUrl && validTermSetId && validAnchorTermId && validExtraAnchorTermIds) {
                this.taxonomyService = new SPTaxonomyService(siteUrl);
            }
        }

        ReactDOM.render(
            React.createElement(Termpicker, {
                taxonomyService: this.taxonomyService,
                termSetId: this.termSetId,
                anchorTermId: this.anchorTermId,
                extraAnchorTermIds: this.extraAnchorTermIds,
                label: context.parameters.Label.raw,
                panelTitle: context.parameters.PanelTitle.raw,
                allowMultipleSelections: context.parameters.AllowMultipleSelections.raw,
                allowSelectingChildren: context.parameters.AllowSelectingChildren.raw,
                initialValues: this.initialValues,
                inputHeight: context.parameters.InputHeight.raw,
                error: context.parameters.Error.raw,
                errorBorderColor: context.parameters.ErrorBorderColor.raw,
                placeHolder: context.parameters.PlaceHolder.raw,
                disabled: context.parameters.Disabled.raw,
                iconColor: context.parameters.IconColor.raw,
                iconSize: context.parameters.IconSize.raw,
                pageSize: context.parameters.PageSize.raw,
                hideDeprecatedTerms: context.parameters.HideDeprecatedTerms.raw,
                checkService: this.checkService,
                validApiUrl,
                validSiteUrl,
                validTermSetId,
                validAnchorTermId,
                validExtraAnchorTermIds,
                onChange: this.onChange,
            }),
            this.container,
        );
    }

    private onChange = (terms: ITermInfo[]): void => {
        if (terms && terms.length > 0) {
            this.termValues = terms.map((t) => { return `-1;#${t.labels[0]?.name}|${t.id}` }).join(';#');
            this.terms = terms.map((t) => { return { id: t.id, labels: t.labels.map((l) => { return { isDefault: l.isDefault, languageTag: l.languageTag, name: l.name } }) } });
        } else {
            // Handle empty array case
            this.termValues = '';
            this.terms = [];
        }
        this.notifyOutputChanged();
    }

    /**
     * It is called by the framework prior to a control receiving new data.
     * @returns an object based on nomenclature defined in manifest, expecting object[s] for property marked as “bound” or “output”
     */
    public getOutputs(): IOutputs {
        return {
            TermValues: this.termValues,
            TermSetId: this.termSetId,
            AnchorTermId: this.anchorTermId,
            ExtraAnchorTermIds: this.extraAnchorTermIds,
            Terms: this.terms,
        };
    }

    public async getOutputSchema(context: ComponentFramework.Context<IInputs>): Promise<Record<string, unknown>> {
        return Promise.resolve({
            TermValues: stringOutPutSchema,
            TermSetId: stringOutPutSchema,
            AnchorTermId: stringOutPutSchema,
            ExtraAnchorTermIds: stringOutPutSchema,
            Terms: termsOutPutSchema,
        });
    }

    /**
     * Called when the control is to be removed from the DOM tree. Controls should use this call for cleanup.
     * i.e. cancelling any pending remote calls, removing listeners, etc.
     */
    public destroy(): void {
        // Add code to cleanup control if necessary;
    }
}

