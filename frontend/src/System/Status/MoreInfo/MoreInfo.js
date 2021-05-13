import React, { Component } from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import FieldSet from 'Components/FieldSet';
import Link from 'Components/Link/Link';
import translate from 'Utilities/String/translate';

class MoreInfo extends Component {

  //
  // Render

  render() {
    return (
      <FieldSet legend={translate('MoreInfo')}>
        <DescriptionList>
          <DescriptionListItemTitle>Home page</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://prowlarr.com/">prowlarr.com</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Discord</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://prowlarr.com/discord">prowlarr.com/discord</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Wiki</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://wiki.servarr.com/Prowlarr">wiki.servarr.com/Prowlarr</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Donations</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://opencollective.com/prowlarr">opencollective.com/prowlarr</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Source</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://github.com/Prowlarr/Prowlarr/">github.com/Prowlarr/Prowlarr</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Feature Requests</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://github.com/Prowlarr/Prowlarr/issues">github.com/Prowlarr/Prowlarr/issues</Link>
          </DescriptionListItemDescription>

        </DescriptionList>
      </FieldSet>
    );
  }
}

MoreInfo.propTypes = {

};

export default MoreInfo;
