import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { IInputs, IOutputs } from "./generated/ManifestTypes";
import { Termpicker } from './components/Termpicker';
import { ITermInfo } from '@pnp/sp/taxonomy';
import { outPutSchema } from './utilities/schemas';
import { SPTaxonomyService } from './services/SPTaxonomyService';
import { Optional } from './controls/modernTaxonomyPicker';
import { getTermValuesArray, validTermValues } from './utilities/common';
import { serviceStatusCheck } from '../services/spServices';

export class TermPickerControl implements ComponentFramework.StandardControl<IInputs, IOutputs> {

    private initialValues: Optional<ITermInfo, "childrenCount" | "createdDateTime" | "lastModifiedDateTime" | "descriptions" | "customSortOrder" | "properties" | "localProperties" | "isDeprecated" | "isAvailableForTagging" | "topicRequested">[];
    private termValues: string; // type (ITermInfo) or Array in case the whole object is needed
    private notifyOutputChanged: () => void;
    private container: HTMLDivElement;
    private context: ComponentFramework.Context<IInputs>;
    private taxonomyService: SPTaxonomyService;
    private previousTermValues: string;

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
    public init(context: ComponentFramework.Context<IInputs>, notifyOutputChanged: () => void, state: ComponentFramework.Dictionary, container: HTMLDivElement): void {
        // Add control initialization code
        this.notifyOutputChanged = notifyOutputChanged;
        this.container = container;
        this.context = context;
        this.context.mode.trackContainerResize(true);
    }

    /**
     * Called when any value in the property bag has changed. This includes field values, data-sets, global values such as container height and width, offline status, control metadata values such as label, visible, etc.
     * @param context The entire property bag available to control via Context Object; It contains values as set up by the customizer mapped to names defined in the manifest, as well as utility functions
     */
    public async updateView(context: ComponentFramework.Context<IInputs>): Promise<void> {

        // storing the latest context from the control.
        this.context = context;

        // Get the current value of the property
        const siteUrl = context.parameters.SiteUrl.raw;
        const currentTermValues = context.parameters.TermValues.raw || '';
        const areTermValuesValid = validTermValues(currentTermValues);

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

        // Check service status
        const checkService = await serviceStatusCheck();

        // Get taxonomy service
        if (!this.taxonomyService) {
            this.taxonomyService = new SPTaxonomyService(siteUrl);
        }

        ReactDOM.render(
            React.createElement(Termpicker, {
                taxonomyService: this.taxonomyService,
                termSetId: context.parameters.TermSetId.raw,
                label: context.parameters.Label.raw,
                panelTitle: context.parameters.PanelTitle.raw,
                allowMultipleSelections: context.parameters.AllowMultipleSelections.raw,
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
                checkService: checkService,
                onChange: this.onChange,
            }),
            this.container,
        );
    }

    private onChange = (terms: ITermInfo[]): void => {
        this.termValues = terms.map((t) => { return `-1;#${t.labels[0]?.name}|${t.id}` }).join(';#');
        this.notifyOutputChanged();
    }

    /**
     * It is called by the framework prior to a control receiving new data.
     * @returns an object based on nomenclature defined in manifest, expecting object[s] for property marked as “bound” or “output”
     */
    public getOutputs(): IOutputs {
        return {
            TermValues: this.termValues,
        };
    }

    public async getOutputSchema(context: ComponentFramework.Context<IInputs>): Promise<Record<string, unknown>> {
        return Promise.resolve({
            TermValues: outPutSchema,
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
