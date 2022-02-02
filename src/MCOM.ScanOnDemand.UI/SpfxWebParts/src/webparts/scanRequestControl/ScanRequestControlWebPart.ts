import * as React from 'react';
import * as ReactDom from 'react-dom';
import { Version } from '@microsoft/sp-core-library';
import {
  IPropertyPaneConfiguration,
  PropertyPaneTextField
} from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';

import * as strings from 'ScanRequestControlWebPartStrings';
import ScanRequestControl from './components/ScanRequestControl';
import { IScanRequestControlProps } from './components/IScanRequestControlProps';

export interface IScanRequestControlWebPartProps {
  appId: string;
  width: string;
  height: string;
}

export default class ScanRequestControlWebPart extends BaseClientSideWebPart<IScanRequestControlWebPartProps> {

  public render(): void {
    const element: React.ReactElement<IScanRequestControlProps> = React.createElement(
      ScanRequestControl,
      {
        appId: this.properties.appId,
        width: this.properties.width,
        height: this.properties.height,
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
                })
              ]
            }
          ]
        }
      ]
    };
  }
}
