import PropTypes from 'prop-types';
import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import FieldSet from 'Components/FieldSet';
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
    indexerUrls,
    fields,
    protocol,
    capabilities,
    onModalClose
  } = props;

  const baseUrl = fields.find((field) => field.name === 'baseUrl')?.value ?? indexerUrls[0];

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {`${name}`}
      </ModalHeader>

      <ModalBody>
        <FieldSet legend={translate('IndexerDetails')}>
          <div className={styles.groups}>
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
              <DescriptionListItemTitle>{translate('IndexerSite')}</DescriptionListItemTitle>
              <DescriptionListItemDescription>
                <Link to={baseUrl}>{baseUrl}</Link>
              </DescriptionListItemDescription>
              <DescriptionListItemTitle>{`${protocol === 'usenet' ? 'Newznab' : 'Torznab'} Url`}</DescriptionListItemTitle>
              <DescriptionListItemDescription>
                {`${window.location.origin}${window.Prowlarr.urlBase}/${id}/api`}
              </DescriptionListItemDescription>
            </DescriptionList>
          </div>
        </FieldSet>
        <FieldSet legend={translate('SearchCapabilities')}>
          <div className={styles.groups}>
            <DescriptionList>
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('RawSearchSupported')}
                data={capabilities.supportsRawSearch ? translate('Yes') : translate('No')}
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('SearchTypes')}
                data={capabilities.searchParams.length === 0 ? translate('NotSupported') : capabilities.searchParams[0]}
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('TVSearchTypes')}
                data={capabilities.tvSearchParams.length === 0 ? translate('NotSupported') : capabilities.tvSearchParams.join(', ')}
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('MovieSearchTypes')}
                data={capabilities.movieSearchParams.length === 0 ? translate('NotSupported') : capabilities.movieSearchParams.join(', ')}
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('BookSearchTypes')}
                data={capabilities.bookSearchParams.length === 0 ? translate('NotSupported') : capabilities.bookSearchParams.join(', ')}
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('MusicSearchTypes')}
                data={capabilities.musicSearchParams.length === 0 ? translate('NotSupported') : capabilities.musicSearchParams.join(', ')}
              />
            </DescriptionList>
          </div>
        </FieldSet>
      </ModalBody>
    </ModalContent >
  );
}

IndexerInfoModalContent.propTypes = {
  id: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  description: PropTypes.string.isRequired,
  encoding: PropTypes.string.isRequired,
  language: PropTypes.string.isRequired,
  indexerUrls: PropTypes.arrayOf(PropTypes.string).isRequired,
  fields: PropTypes.arrayOf(PropTypes.object).isRequired,
  protocol: PropTypes.string.isRequired,
  capabilities: PropTypes.object.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default IndexerInfoModalContent;
