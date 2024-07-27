import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import FieldSet from 'Components/FieldSet';
import Link from 'Components/Link/Link';
import translate from 'Utilities/String/translate';

function MoreInfo() {
  return (
    <FieldSet legend={translate('MoreInfo')}>
      <DescriptionList>
        <DescriptionListItemTitle>
          {translate('HomePage')}
        </DescriptionListItemTitle>
        <DescriptionListItemDescription>
          <Link to="https://prowlarr.com/">prowlarr.com</Link>
        </DescriptionListItemDescription>

        <DescriptionListItemTitle>{translate('Wiki')}</DescriptionListItemTitle>
        <DescriptionListItemDescription>
          <Link to="https://wiki.servarr.com/prowlarr">
            wiki.servarr.com/prowlarr
          </Link>
        </DescriptionListItemDescription>

        <DescriptionListItemTitle>
          {translate('Reddit')}
        </DescriptionListItemTitle>
        <DescriptionListItemDescription>
          <Link to="https://reddit.com/r/prowlarr">r/prowlarr</Link>
        </DescriptionListItemDescription>

        <DescriptionListItemTitle>
          {translate('Discord')}
        </DescriptionListItemTitle>
        <DescriptionListItemDescription>
          <Link to="https://prowlarr.com/discord">prowlarr.com/discord</Link>
        </DescriptionListItemDescription>

        <DescriptionListItemTitle>
          {translate('Source')}
        </DescriptionListItemTitle>
        <DescriptionListItemDescription>
          <Link to="https://github.com/Prowlarr/Prowlarr/">
            github.com/Prowlarr/Prowlarr
          </Link>
        </DescriptionListItemDescription>

        <DescriptionListItemTitle>
          {translate('FeatureRequests')}
        </DescriptionListItemTitle>
        <DescriptionListItemDescription>
          <Link to="https://github.com/Prowlarr/Prowlarr/issues">
            github.com/Prowlarr/Prowlarr/issues
          </Link>
        </DescriptionListItemDescription>
      </DescriptionList>
    </FieldSet>
  );
}

export default MoreInfo;
