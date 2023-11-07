import * as React from 'react';
import * as ReactDom from 'react-dom';
import { Version } from '@microsoft/sp-core-library';
import {
  IPropertyPaneConfiguration,
  IPropertyPaneField,
  IPropertyPaneLabelProps,
  IPropertyPaneTextFieldProps,
  PropertyPaneLabel,
  PropertyPaneTextField,
  PropertyPaneToggle
} from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';

import * as strings from 'ScanRequestControlWebPartStrings';
import ScanRequestControl from './components/ScanRequestControl';
import { IScanRequestControlProps } from './components/IScanRequestControlProps';

export interface IScanRequestControlWebPartProps {
  appId: string;
  width: string;
  height: string;
  checkAppPermissions: boolean;
  checkAppPermissionsMessage: string;
}

export default class ScanRequestControlWebPart extends BaseClientSideWebPart<IScanRequestControlWebPartProps> {

  public render(): void {
    const { appId, width, height, checkAppPermissions, checkAppPermissionsMessage } = this.properties;
    const element: React.ReactElement<IScanRequestControlProps> = React.createElement(
      ScanRequestControl,
      {
        appId,
        width,
        height,
        wpContext: this.context,
        checkAppPermissions,
        checkAppPermissionsMessage,
      }
    );

    ReactDom.render(element, this.domElement);
  }

  protected onDispose(): void {
    ReactDom.unmountComponentAtNode(this.domElement);
  }

  protected get dataVersion(): Version {
    return Version.parse('1.0');
  }

  protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration {
    let templateProperty: IPropertyPaneField<IPropertyPaneTextFieldProps | IPropertyPaneLabelProps>;

    if (this.properties.checkAppPermissions) {
      templateProperty = PropertyPaneTextField('checkAppPermissionsMessage', {
        label: strings.CheckAppPermissionsMessageFieldLabel,
        multiline: true,
      });
    } else {
      templateProperty = PropertyPaneLabel('', {
        text: ''
      });
    }

    return {
      pages: [
        {
          header: {
            description: strings.PropertyPaneDescription
          },
          groups: [
            {
              groupName: strings.BasicGroupName,
              groupFields: [
                PropertyPaneTextField('appId', {
                  label: strings.ApplicationIdFieldLabel,
                }),
                PropertyPaneTextField('width', {
                  label: strings.WidthFieldLabel,
                }),
                PropertyPaneTextField('height', {
                  label: strings.HeightFieldLabel
                }),
                PropertyPaneToggle('checkAppPermissions', {
                  label: strings.CheckAppPermissionsFieldLabel
                }),
                templateProperty
              ]
            }
          ]
        }
      ]
    };
  }
}
