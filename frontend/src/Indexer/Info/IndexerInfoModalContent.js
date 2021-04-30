import PropTypes from 'prop-types';
import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import Link from 'Components/Link/Link';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalHeader from 'Components/Modal/ModalHeader';
import translate from 'Utilities/String/translate';
import styles from './IndexerInfoModalContent.css';

function IndexerInfoModalContent(props) {
  const {
    id,
    name,
    description,
    encoding,
    language,
    baseUrl,
    protocol,
    onModalClose
  } = props;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {`${name}`}
      </ModalHeader>

      <ModalBody>
        <DescriptionList>
          <DescriptionListItem
            descriptionClassName={styles.description}
            title={translate('Id')}
            data={id}
          />
          <DescriptionListItem
            descriptionClassName={styles.description}
            title={translate('Description')}
            data={description ? description : '-'}
          />
          <DescriptionListItem
            descriptionClassName={styles.description}
            title={translate('Encoding')}
            data={encoding ? encoding : '-'}
          />
          <DescriptionListItem
            descriptionClassName={styles.description}
            title={translate('Language')}
            data={language ?? '-'}
          />

          <DescriptionListItemTitle>Indexer Site</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to={baseUrl}>{baseUrl}</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>{protocol === 'usenet' ? 'Newznab' : 'Torznab'} Url</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            {`${window.location.origin}${window.Prowlarr.apiRoot}/${id}/api`}
          </DescriptionListItemDescription>

        </DescriptionList>
      </ModalBody>
    </ModalContent>
  );
}

IndexerInfoModalContent.propTypes = {
  id: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  description: PropTypes.string.isRequired,
  encoding: PropTypes.string.isRequired,
  language: PropTypes.string.isRequired,
  baseUrl: PropTypes.string.isRequired,
  protocol: PropTypes.string.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default IndexerInfoModalContent;
