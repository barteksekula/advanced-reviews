interface ExternalReviewResources_Component {
    /** External review links */
    title: string;
    /** Manage list of external review links */
    description: string;
}

interface ExternalReviewResources_Removedialog {
    /** Cancel */
    cancel: string;
    /** Are you sure you want to remove link? */
    description: string;
    /** Ok */
    ok: string;
    /** Remove link */
    title: string;
}

interface ExternalReviewResources_List_Editdialog {
    /** Edit link */
    title: string;
    /** Add link */
    newitemtitle: string;
    /** PIN code */
    pincode: string;
    /** digits */
    digits: string;
    /** Display name */
    displayname: string;
    /** Update PIN code */
    pincheckboxlabel: string;
    /** Link secured with PIN code */
    linksecured: string;
    /** Link with no PIN code security */
    linknotsecured: string;
    /** Optional link name */
    displaynamehelptext: string;
    /** Visitor groups */
    visitorgroups: string;
    /** Impersonate as selected visitor groups */
    visitorgroupshelptext: string;
    /** Valid to */
    validto: string;
    /** Prolong by [#days#] days */
    prolongbydays: string;
}

interface ExternalReviewResources_List {
    /** Project */
    projectname: string;
    /** delete */
    deletetitle: string;
    /** There are no external links for this content */
    emptylist: string;
    /** Valid to */
    itemvalidto: string;
    /** share */
    sharetitle: string;
    /** View */
    viewlink: string;
    /** Edit */
    editlink: string;
    editdialog: ExternalReviewResources_List_Editdialog;
}

interface ExternalReviewResources_Sharedialog {
    /** Cancel */
    cancelbutton: string;
    /** Share link to the content with external editor */
    dialogtitle: string;
    /** Email message */
    emailmessagelabel: string;
    /** Email subject */
    emailsubjectlabel: string;
    /** Email address */
    emailaddresslabel: string;
    /** Text [#link#] will be replaced with link to content. */
    messagehint: string;
    /** Send */
    sendbutton: string;
}

interface ExternalReviewResources_Shared {
    /** Ok */
    ok: string;
    /** Cancel */
    cancel: string;
    /** Save */
    save: string;
}

interface ExternalReviewResources {
    component: ExternalReviewResources_Component;
    removedialog: ExternalReviewResources_Removedialog;
    list: ExternalReviewResources_List;
    sharedialog: ExternalReviewResources_Sharedialog;
    shared: ExternalReviewResources_Shared;
}
