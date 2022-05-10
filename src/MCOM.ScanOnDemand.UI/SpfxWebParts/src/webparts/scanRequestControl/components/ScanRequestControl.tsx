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
  private itemid = this.urlParams.get('id');
  private fileName = this.urlParams.get('name');
  private siteId = this.urlParams.get('sid');
  private webId = this.urlParams.get('wid');
  private listId = this.urlParams.get('lid');

  public render(): React.ReactElement<IScanRequestControlProps> {
    return (
      <div className={styles.scanRequestControl}>
        <div className={styles.container}>
          <div className={styles.row} style={{backgroundColor: '#fff'}}>
            <div className={styles.column} style={{width: '100%', position: 'initial'}}>
              {this.appId &&
                <div style={{paddingTop: '56.2%', position: 'relative'}}>
                  <iframe frameBorder="0" style={{backgroundColor: '#FFFFFF', overflow: 'hidden', height: '100%', width: '100%', position: 'absolute', top: '0', left: '0' }} width={this.width} height={this.height} src={`https://web.powerapps.com/webplayer/iframeapp?source=iframe&screenColor=rgba(104,101,171,1)&id=${this.itemid}&name=${this.fileName}&sid=${this.siteId}&wid=${this.webId}&lid=${this.listId}&appId=/providers/Microsoft.PowerApps/apps/${this.appId}`}></iframe>
                </div>
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
