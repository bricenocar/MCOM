import { WebPartContext } from "@microsoft/sp-webpart-base";

export interface IScanRequestControlProps {
  appId: string;
  width: string;
  height: string;
  wpContext: WebPartContext;
  checkAppPermissions: boolean;
  checkAppPermissionsMessage: string;
}
