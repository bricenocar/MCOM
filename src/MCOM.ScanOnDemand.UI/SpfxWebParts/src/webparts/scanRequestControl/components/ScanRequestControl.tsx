import * as React from 'react';
import styles from './ScanRequestControl.module.scss';
import { IScanRequestControlProps } from './IScanRequestControlProps';

export default class ScanRequestControl extends React.Component<IScanRequestControlProps, {}> {

  // Get props
  private appId = this.props.appId;
  private width = this.props.width ?? '100%';
  private height = this.props.height ?? '100%';

  // Get querystring values
  private queryString = window.location.search;
  private urlParams = new URLSearchParams(this.queryString);
  private itemid = this.urlParams.get('itemid');
  private fileName = this.urlParams.get('filename');

  public render(): React.ReactElement<IScanRequestControlProps> {
    return (
      <div className={styles.scanRequestControl}>
        <div className={styles.container}>
          <div className={styles.row}>
            <div className={styles.column}>
              {this.appId &&
                <iframe frameBorder="0" style={{ overflow: 'hidden', height: '100%', width: '100%', minHeight: '470px' }} width={this.width} height={this.height} src={`https://web.powerapps.com/webplayer/iframeapp?source=iframe&screenColor=rgba(104,101,171,1)&itemid=${this.itemid}&filename=${this.fileName}&appId=/providers/Microsoft.PowerApps/apps/${this.appId}`}></iframe>
              }
              {!this.appId &&
                <div>
                  <span className={styles.title}>Missing ApplicationId!</span>
                  <p className={styles.subTitle}>The application id is required in order to show the iframe.</p>
                  <p className={styles.description}>Please add the appid in the webpart properties</p>
                </div>
              }
            </div>
          </div>
        </div>
      </div>
    );
  }
}
