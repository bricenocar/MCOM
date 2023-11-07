import * as React from 'react';
import styles from './ScanRequestControl.module.scss';

import { IScanRequestControlProps } from './IScanRequestControlProps';
import { IScanRequestControlState } from './IScanRequestControlState';
import { HttpClient } from '@microsoft/sp-http';
import { Shimmer, ShimmerElementsGroup, ShimmerElementType } from 'office-ui-fabric-react/lib/Shimmer';
import { ServiceGetPowerAppPermissionsUrl_Prod, ServiceGetPowerAppPermissionsUrl_Test, TenantUrl_Prod } from '../../../utilities/services';

import '@pnp/sp/webs';
import '@pnp/sp/site-users';
import { getStorageItem, setStorageItem } from '../../../utilities/localStorage';

export default class ScanRequestControl extends React.Component<IScanRequestControlProps, IScanRequestControlState> {

  // Session
  private storageKey = 'mcom-powerapp-access';

  // Get props
  private appId = this.props.appId;
  private width = this.props.width ?? '100%';
  private height = this.props.height ?? '100%';
  private checkAppPermissions = this.props.checkAppPermissions;
  private checkAppPermissionsMessage = this.props.checkAppPermissionsMessage;

  // Get querystring values
  private queryString = window.location.search;
  private urlParams = new URLSearchParams(this.queryString);
  private itemid = this.urlParams.get('iid');
  private fileName = this.urlParams.get('name');
  private fileId = this.urlParams.get('rid');
  private siteId = this.urlParams.get('sid');
  private webId = this.urlParams.get('wid');
  private listId = this.urlParams.get('lid');

  constructor(props: IScanRequestControlProps) {
    super(props);
    // set initial state
    this.state = {
      userIdLoaded: false,
      doesUserHaveAccessToApp: false
    };
  }

  

  private async doesUserHavePermissionsToApp(): Promise<boolean> {
    // Init sp
    const { wpContext } = this.props;
    const userId = wpContext.pageContext.user.loginName.toLowerCase();
    const appId = this.appId;

    // Get storage value    
    const storageItemValue = getStorageItem(this.storageKey);

    if (storageItemValue && storageItemValue === 'true') {
      this.setState({ ...this.state, userIdLoaded: true, doesUserHaveAccessToApp: true });
    }
    else {
      // Validate
      if (!userId || !this.appId) return false;

      // Get current site url
      const currentUrl = wpContext.pageContext.site.absoluteUrl;

      // Build url and headers
      const url = (currentUrl.indexOf(TenantUrl_Prod) > 0) ? `${ServiceGetPowerAppPermissionsUrl_Prod}&userId=${userId}&appId=${appId}` : `${ServiceGetPowerAppPermissionsUrl_Test}&userId=${userId}&appId=${appId}`;

      // Request Headers
      const headers: Headers = new Headers();
      headers.append('Content-type', 'application/json');
      headers.append('Cache-Control', 'no-cache');

      // Request Options
      const httpClientOptions = {
        headers
      };

      // Send request
      const httpRequest = await wpContext.httpClient.get(url, HttpClient.configurations.v1, httpClientOptions);

      // Get response
      const doesUserHaveAccessToApp = await httpRequest.json();

      if (doesUserHaveAccessToApp) {
        const expire = 172800000; // 2 days
        setStorageItem(this.storageKey, 'true', expire);
      }

      this.setState({ ...this.state, userIdLoaded: true, doesUserHaveAccessToApp });
    }

    return true;
  }

  public componentDidMount = (): void => {
    if (this.checkAppPermissions) {
      this.doesUserHavePermissionsToApp().catch(e => console.log(e));
    } else {
      this.setState({ ...this.state, userIdLoaded: true, doesUserHaveAccessToApp: true });
    }
  }

  private getShimmerElements = (): JSX.Element => {
    return (
      <div>
        <div className={styles.shimmerGroup}>
          <ShimmerElementsGroup
            shimmerElements={[
              { type: ShimmerElementType.line, width: "100%", height: 250 },
            ]}
          />
          <ShimmerElementsGroup
            shimmerElements={[
              { type: ShimmerElementType.gap, width: "100%", height: 30 },
            ]}
          />
        </div>
      </div>
    );
  }

  private replaceHtmlContent = (): string => {
    return this.checkAppPermissionsMessage.replace('{filename}', this.fileName).replace('{fileid}', this.fileId);
  }

  public render(): React.ReactElement<IScanRequestControlProps> {
    const { userIdLoaded, doesUserHaveAccessToApp } = this.state;
    const htmlMessage = (this.checkAppPermissions) ? this.replaceHtmlContent() : '';

    // Check wheter the current user has access to the PowerApp or not...
    if (this.appId) {
      if (userIdLoaded) {
        if (doesUserHaveAccessToApp) {
          return (
            <div className={styles.scanRequestControl}>
              <div className={styles.container}>
                <div className={styles.row} style={{ backgroundColor: '#fff' }}>
                  <div className={styles.column} style={{ width: '100%', position: 'initial' }}>
                    <div style={{ paddingTop: '56.2%', position: 'relative' }}>
                      <iframe frameBorder="0" style={{ backgroundColor: '#FFFFFF', overflow: 'hidden', height: '100%', width: '100%', position: 'absolute', top: '0', left: '0' }} width={this.width} height={this.height} src={`https://web.powerapps.com/webplayer/iframeapp?source=iframe&screenColor=rgba(104,101,171,1)&iid=${this.itemid}&name=${this.fileName}&sid=${this.siteId}&wid=${this.webId}&lid=${this.listId}&appId=/providers/Microsoft.PowerApps/apps/${this.appId}`} />
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )
        }
        else {
          return (
            <div className={styles.scanRequestControl}>
              <div className={styles.container}>
                <div className={styles.row} style={{ backgroundColor: '#fff' }}>
                  <div className={styles.column} style={{ width: '100%', position: 'initial', color: 'black' }}>
                    <div dangerouslySetInnerHTML={{ __html: htmlMessage }} />
                  </div>
                </div>
              </div >
            </div >
          )
        }
      } else {
        return (
          <Shimmer className={"ms-Grid-col ms-sm12 ms-md6 ms-lg3"} isDataLoaded={userIdLoaded} customElementsGroup={this.getShimmerElements()} />
        )
      }
    }
    else {
      return (
        <div>
          <span className={styles.title}><b>Missing ApplicationId!</b></span>
          <p className={styles.subTitle}>The application id is required in order to show the iframe.</p>
          <p className={styles.description}>Please add the appid in the webpart properties</p>
        </div>
      )
    }
  }
}
