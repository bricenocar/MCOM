import { override } from '@microsoft/decorators';
import { Log } from '@microsoft/sp-core-library';
import { Dialog } from '@microsoft/sp-dialog';
import {
  BaseListViewCommandSet,
  Command,
  IListViewCommandSetListViewUpdatedParameters,
  IListViewCommandSetExecuteEventParameters
} from '@microsoft/sp-listview-extensibility';

export interface IFileProps {
  FileName?: string;
  FileURL?: string;
  FileType?: string;
  IsFile?: boolean;
  ID?: string;
  UniqueID?: any;
  ValidFileType?: boolean;
}

export interface IScanRequestActionCommandSetProperties {
  targetUrl: string;
}

const LOG_SOURCE: string = 'ScanRequestActionCommandSet';

export default class ScanRequestActionCommandSet extends BaseListViewCommandSet<IScanRequestActionCommandSetProperties> {

  private fileInfo: IFileProps = {};

  @override
  public onInit(): Promise<void> {
    Log.info(LOG_SOURCE, 'Initialized ScanRequestActionCommandSet');
    return Promise.resolve();
  }

  @override
  public onListViewUpdated(event: IListViewCommandSetListViewUpdatedParameters): void {
    const compareOneCommand: Command = this.tryGetCommand('ScanOnDemand');
    if (compareOneCommand) {
      // Check if only one row is selected
      const visible = event.selectedRows.length === 1;

      // This command should be hidden unless exactly one row is selected.
      compareOneCommand.visible = visible;

      // Get file info
      if (visible) {
        this.fileInfo = {
          FileType: event.selectedRows[0].getValueByName('File_x0020_Type'),
          IsFile: event.selectedRows[0].getValueByName('FSObjType') == "0",
          ID: event.selectedRows[0].getValueByName('ID'),
          UniqueID: event.selectedRows[0].getValueByName('UniqueId'),
          FileName: event.selectedRows[0].getValueByName('FileLeafRef'),
          FileURL: event.selectedRows[0].getValueByName('FileRef')
        };
      }
    }
  }

  @override
  public onExecute(event: IListViewCommandSetExecuteEventParameters): void {
    switch (event.itemId) {
      case 'ScanOnDemand':
        const itemId: string = event.selectedRows[0].getValueByName("ID");      
        const url: string = this.properties.targetUrl.replace('{id}', itemId);     

        window.open(url, '_blank').focus();
        break;
      default:
        throw new Error('Unknown command');
    }
  }
}
