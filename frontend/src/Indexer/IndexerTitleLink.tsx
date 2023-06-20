import PropTypes from 'prop-types';
import React, { useCallback, useState } from 'react';
import Link from 'Components/Link/Link';
import IndexerInfoModal from './Info/IndexerInfoModal';
import styles from './IndexerTitleLink.css';

interface IndexerTitleLinkProps {
  indexerName: string;
  indexerId: number;
}

function IndexerTitleLink(props: IndexerTitleLinkProps) {
  const { indexerName, indexerId } = props;

  const [isIndexerInfoModalOpen, setIsIndexerInfoModalOpen] = useState(false);

  const onIndexerInfoPress = useCallback(() => {
    setIsIndexerInfoModalOpen(true);
  }, [setIsIndexerInfoModalOpen]);

  const onIndexerInfoModalClose = useCallback(() => {
    setIsIndexerInfoModalOpen(false);
  }, [setIsIndexerInfoModalOpen]);

  return (
    <div>
      <Link className={styles.link} onPress={onIndexerInfoPress}>
        {indexerName}
      </Link>

      <IndexerInfoModal
        indexerId={indexerId}
        isOpen={isIndexerInfoModalOpen}
        onModalClose={onIndexerInfoModalClose}
      />
    </div>
  );
}

IndexerTitleLink.propTypes = {
  indexerName: PropTypes.string.isRequired,
};

export default IndexerTitleLink;
